import base64
from datetime import date, datetime

import pytz
from lxml import etree

from odoo import fields, models
from odoo.exceptions import UserError

RUTA_XSD_IDA = "/mnt/gentefit-xsd/lote_sesiones_ida.xsd"
ZONA_ESPANA = pytz.timezone("Europe/Madrid")


class GentefitImportarLote(models.TransientModel):
    _name = "gentefit.importar.lote"
    _description = "Asistente para importar un lote de sesiones de GenteFit"

    fichero = fields.Binary(string="Fichero XML", required=True)
    nombre_fichero = fields.Char(string="Nombre del fichero")
    resultado = fields.Text(string="Resultado", readonly=True)

    def action_importar(self):
        documento = self._leer_y_validar_xml()            # niveles A y B
        validas, rechazadas = self._comprobar_reglas(documento)  # nivel C
        creadas, actualizadas = self._guardar_sesiones(validas)
        self.resultado = self._redactar_informe(documento, creadas, actualizadas, rechazadas)

        # Vuelve a abrir la misma ventana para que se vea el informe
        return {
            "type": "ir.actions.act_window",
            "res_model": self._name,
            "res_id": self.id,
            "view_mode": "form",
            "target": "new",
        }

    # ------------------------------------------------------------------
    # Niveles A y B: el fichero
    # ------------------------------------------------------------------
    def _leer_y_validar_xml(self):
        """Niveles A y B. Devuelve el XML ya leído o lanza UserError (lote rechazado entero)."""
        contenido = base64.b64decode(self.fichero)

        # --- Nivel A: bien formado ---
        parser = etree.XMLParser(resolve_entities=False, no_network=True)
        try:
            documento = etree.fromstring(contenido, parser)
        except etree.XMLSyntaxError as error:
            raise UserError(f"Lote rechazado: el fichero no es un XML bien formado.\n{error}")

        # --- Nivel B: válido según el XSD ---
        try:
            esquema = etree.XMLSchema(etree.parse(RUTA_XSD_IDA))
        except (OSError, etree.XMLSchemaParseError) as error:
            raise UserError(f"No se ha podido cargar el XSD de ida ({RUTA_XSD_IDA}).\n{error}")

        if not esquema.validate(documento):
            errores = "\n".join(f"Línea {e.line}: {e.message}" for e in esquema.error_log)
            raise UserError(f"Lote rechazado: el fichero no cumple el XSD de ida.\n{errores}")

        return documento

    # ------------------------------------------------------------------
    # Nivel C: reglas de negocio
    # ------------------------------------------------------------------
    def _comprobar_reglas(self, documento):
        """Nivel C. Devuelve (validas, rechazadas).
        validas: lista de diccionarios listos para guardar.
        rechazadas: lista de (sesion, motivo)."""
        cabecera = documento.find("cabecera")
        nodos_sesion = documento.findall("sesiones/sesion")

        # Regla 1: el total declarado tiene que coincidir. Si no, fuera el lote entero
        total = int(cabecera.findtext("total_registros"))
        if total != len(nodos_sesion):
            raise UserError(
                f"Lote rechazado: la cabecera dice {total} sesiones "
                f"pero el fichero trae {len(nodos_sesion)}."
            )

        desde = date.fromisoformat(cabecera.findtext("periodo_desde"))
        hasta = date.fromisoformat(cabecera.findtext("periodo_hasta"))

        validas, rechazadas = [], []
        for nodo in nodos_sesion:
            sesion = self._leer_sesion(nodo)

            # Regla 2: no puede haber más plazas ocupadas que el límite
            if sesion["plazas_ocupadas"] > sesion["plazas_limite"]:
                rechazadas.append((sesion,
                    f"plazas ocupadas ({sesion['plazas_ocupadas']}) mayor que el límite "
                    f"({sesion['plazas_limite']})"))
                continue

            # Regla 3: la fecha tiene que estar dentro del periodo del lote (incluidos los extremos)
            if not desde <= sesion["fecha"].date() <= hasta:
                rechazadas.append((sesion,
                    f"fecha {sesion['fecha']:%d/%m/%Y} fuera del periodo "
                    f"{desde:%d/%m/%Y} - {hasta:%d/%m/%Y}"))
                continue

            validas.append(sesion)

        return validas, rechazadas

    def _leer_sesion(self, nodo):
        """Convierte un <sesion> del XML en un diccionario con los nombres de campo del modelo."""
        return {

            "gentefit_id": nodo.findtext("gentefit_id").lower(),
            "fecha": datetime.fromisoformat(nodo.findtext("fecha")[:19]),
            "duracion": int(nodo.findtext("duracion")),
            "estado": nodo.findtext("estado"),
            "actividad_ref": nodo.find("actividad").get("id"),
            "actividad_nombre": nodo.findtext("actividad").strip(),
            "intensidad": nodo.findtext("intensidad"),
            "sala_ref": nodo.find("sala").get("id"),
            "sala_nombre": nodo.findtext("sala").strip(),
            "entrenador_ref": nodo.find("entrenador").get("id"),
            "entrenador_nombre": nodo.findtext("entrenador").strip(),
            "plazas_ocupadas": int(nodo.findtext("plazas_ocupadas")),
            "plazas_limite": int(nodo.findtext("plazas_limite")),
        }

    # ------------------------------------------------------------------
    # Guardar: upsert por gentefit_id
    # ------------------------------------------------------------------
    def _guardar_sesiones(self, validas):
        """Crea o actualiza cada sesión válida. Devuelve (creadas, actualizadas)."""

        Sesion = self.env["gentefit.sesion"].with_context(gentefit_importando=True)
        creadas, actualizadas = [], []

        for sesion in validas:
            valores = dict(sesion)  
            valores["fecha"] = self._hora_espana_a_utc(sesion["fecha"])

            # Upsert: buscar por la referencia externa; si existe se actualiza, si no se crea
            existente = Sesion.search([("gentefit_id", "=", sesion["gentefit_id"])], limit=1)
            if existente:
                existente.write(valores)
                actualizadas.append(sesion)
            else:
                Sesion.create(valores)
                creadas.append(sesion)

        return creadas, actualizadas

    @staticmethod
    def _hora_espana_a_utc(fecha_local):
        """2026-11-30 19:00 (España) -> 2026-11-30 18:00 (UTC), sin zona, que es como la guarda Odoo.
        pytz tiene en cuenta el horario de verano (UTC+2) y el de invierno (UTC+1)."""
        return ZONA_ESPANA.localize(fecha_local).astimezone(pytz.utc).replace(tzinfo=None)

    # ------------------------------------------------------------------
    # Informe final
    # ------------------------------------------------------------------
    def _redactar_informe(self, documento, creadas, actualizadas, rechazadas):
        id_lote = documento.findtext("cabecera/id_lote")
        lineas = [
            f"Lote {id_lote} importado.",
            "",
            f"Creadas: {len(creadas)}",
            f"Actualizadas: {len(actualizadas)}",
            f"Rechazadas: {len(rechazadas)}",
        ]
        for sesion, motivo in rechazadas:
            lineas.append(
                f"  - {sesion['actividad_nombre']} del {sesion['fecha']:%d/%m/%Y %H:%M} "
                f"({sesion['gentefit_id']}): {motivo}"
            )
        return "\n".join(lineas)
