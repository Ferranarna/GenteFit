import base64
import re
from datetime import datetime, time, timedelta

import pytz
from lxml import etree

from odoo import fields, models
from odoo.exceptions import UserError

RUTA_XSD_VUELTA = "/mnt/gentefit-xsd/lote_sesiones_vuelta.xsd"
ZONA_ESPANA = pytz.timezone("Europe/Madrid")
FORMATO_FECHA_HORA = "%Y-%m-%dT%H:%M:%S"


class GentefitExportarLote(models.TransientModel):
    _name = "gentefit.exportar.lote"
    _description = "Asistente para exportar un lote de sesiones a GenteFit"

    periodo_desde = fields.Date(
        string="Desde", required=True, default=fields.Date.context_today)
    periodo_hasta = fields.Date(
        string="Hasta", required=True,
        default=lambda self: fields.Date.context_today(self) + timedelta(days=6))
    fichero = fields.Binary(string="Fichero XML", readonly=True)
    nombre_fichero = fields.Char(string="Nombre del fichero", readonly=True)
    resultado = fields.Text(string="Resultado", readonly=True)

    def action_exportar(self):
        if self.periodo_hasta < self.periodo_desde:
            raise UserError("La fecha 'Hasta' no puede ser anterior a 'Desde'.")

        sesiones = self._buscar_pendientes()
        if not sesiones:
            raise UserError(
                "No hay sesiones pendientes de enviar en ese periodo: "
                "no se ha generado ningún fichero.")

        ahora = datetime.now(ZONA_ESPANA).replace(tzinfo=None)
        id_lote = self._siguiente_id_lote(ahora.date())
        documento = self._crear_xml(id_lote, ahora, sesiones)
        self._validar(documento, sesiones)   # si falla, no se cambia nada

        contenido = etree.tostring(
            documento, xml_declaration=True, encoding="UTF-8", pretty_print=True)
        self.write({
            "fichero": base64.b64encode(contenido),
            "nombre_fichero": f"{id_lote}.xml",
            "resultado": (
                f"Lote {id_lote} generado.\n"
                f"Sesiones exportadas: {len(sesiones)}\n"
                "Descarga el fichero e impórtalo en GenteFit."),
        })
        sesiones.write({"pendiente_envio": False})

        # Vuelve a abrir la misma ventana para poder descargar el fichero
        return {
            "type": "ir.actions.act_window",
            "name": "Exportar sesiones",
            "res_model": self._name,
            "res_id": self.id,
            "view_mode": "form",
            "target": "new",
        }

    def action_descargar(self):
        self.ensure_one()
        return {
            "type": "ir.actions.act_url",
            "url": f"/web/content/{self._name}/{self.id}/fichero/{self.nombre_fichero}?download=true",
            "target": "self",
        }

    # ------------------------------------------------------------------
    # Qué se exporta
    # ------------------------------------------------------------------
    def _buscar_pendientes(self):
        """Sesiones pendientes cuya fecha (hora de España) cae en el periodo, extremos incluidos."""
        inicio = self._hora_espana_a_utc(datetime.combine(self.periodo_desde, time.min))
        fin = self._hora_espana_a_utc(
            datetime.combine(self.periodo_hasta + timedelta(days=1), time.min))
        return self.env["gentefit.sesion"].search([
            ("pendiente_envio", "=", True),
            ("fecha", ">=", inicio),
            ("fecha", "<", fin),
        ], order="fecha")

    def _siguiente_id_lote(self, hoy):
        """OD-AAAA-MM-DD-NNN, con un contador que empieza en 001 cada día."""
        parametros = self.env["ir.config_parameter"].sudo()
        clave = f"gentefit.lotes.{hoy:%Y-%m-%d}"
        numero = int(parametros.get_param(clave, "0")) + 1
        if numero > 999:
            raise UserError("Se ha alcanzado el máximo de 999 lotes en un día.")
        parametros.set_param(clave, str(numero))
        return f"OD-{hoy:%Y-%m-%d}-{numero:03d}"

    # ------------------------------------------------------------------
    # El XML
    # ------------------------------------------------------------------
    def _crear_xml(self, id_lote, generado, sesiones):
        raiz = etree.Element("lote_sesiones", version="1.0")

        cabecera = etree.SubElement(raiz, "cabecera")
        self._anadir(cabecera, "id_lote", id_lote)
        self._anadir(cabecera, "sentido", "Odoo_a_GenteFit")
        self._anadir(cabecera, "generado", generado.strftime(FORMATO_FECHA_HORA))
        self._anadir(cabecera, "periodo_desde", self.periodo_desde.isoformat())
        self._anadir(cabecera, "periodo_hasta", self.periodo_hasta.isoformat())
        self._anadir(cabecera, "total_registros", str(len(sesiones)))

        nodo_sesiones = etree.SubElement(raiz, "sesiones")
        for sesion in sesiones:
            nodo = etree.SubElement(nodo_sesiones, "sesion")
            fecha = self._utc_a_hora_espana(sesion.fecha)
            self._anadir(nodo, "gentefit_id", sesion.gentefit_id)
            self._anadir(nodo, "fecha", fecha.strftime(FORMATO_FECHA_HORA))
            self._anadir(nodo, "duracion", str(sesion.duracion))
            self._anadir(nodo, "estado", sesion.estado)
            self._anadir(nodo, "actividad", sesion.actividad_nombre, sesion.actividad_ref)
            self._anadir(nodo, "sala", sesion.sala_nombre, sesion.sala_ref)
            self._anadir(nodo, "entrenador", sesion.entrenador_nombre, sesion.entrenador_ref)
        return raiz

    @staticmethod
    def _anadir(padre, nombre, texto, referencia=None):
        """Crea <nombre>texto</nombre>, o <nombre id="...">texto</nombre> si lleva referencia."""
        nodo = etree.SubElement(padre, nombre)
        if referencia is not None:
            nodo.set("id", referencia)
        nodo.text = texto
        return nodo

    def _validar(self, documento, sesiones):
        try:
            esquema = etree.XMLSchema(etree.parse(RUTA_XSD_VUELTA))
        except (OSError, etree.XMLSchemaParseError) as error:
            raise UserError(
                f"No se ha podido cargar el XSD de vuelta ({RUTA_XSD_VUELTA}).\n{error}")

        if esquema.validate(documento):
            return

        errores = []
        for error in esquema.error_log:
            donde = ""
            posicion = re.search(r"sesion(?:\[(\d+)\])?/", getattr(error, "path", None) or "")
            if posicion:
                sesion = sesiones[int(posicion.group(1) or 1) - 1]
                donde = f"{sesion.actividad_nombre}: "
            errores.append(donde + error.message)
        raise UserError(
            "No se ha generado el lote porque no cumple el XSD de vuelta. Revisa los Id "
            "de actividad, sala y entrenador y la duración (15 a 180 minutos).\n"
            + "\n".join(errores))

    # ------------------------------------------------------------------
    # Horas: Odoo guarda en UTC y el XML lleva hora de España
    # ------------------------------------------------------------------
    @staticmethod
    def _hora_espana_a_utc(fecha_local):
        return ZONA_ESPANA.localize(fecha_local).astimezone(pytz.utc).replace(tzinfo=None)

    @staticmethod
    def _utc_a_hora_espana(fecha_utc):
        return pytz.utc.localize(fecha_utc).astimezone(ZONA_ESPANA).replace(tzinfo=None)