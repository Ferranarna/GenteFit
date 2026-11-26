import base64
from datetime import date, datetime

from lxml import etree

from odoo import fields, models
from odoo.exceptions import UserError

# El XSD de ida, montado desde odoo/xsd por el docker-compose.yml (solo lectura)
RUTA_XSD_IDA = "/mnt/gentefit-xsd/lote_sesiones_ida.xsd"


class GentefitImportarLote(models.TransientModel):
    _name = "gentefit.importar.lote"
    _description = "Asistente para importar un lote de sesiones de GenteFit"

    fichero = fields.Binary(string="Fichero XML", required=True)
    nombre_fichero = fields.Char(string="Nombre del fichero")
    resultado = fields.Text(string="Resultado", readonly=True)

    def action_importar(self):
        documento = self._leer_y_validar_xml()
        validas, rechazadas = self._comprobar_reglas(documento)

        # De momento solo lo contamos. En el paso 5 guardaremos las válidas
        lineas = [f"Sesiones correctas: {len(validas)}", f"Sesiones rechazadas: {len(rechazadas)}"]
        for gentefit_id, motivo in rechazadas:
            lineas.append(f"  - {gentefit_id}: {motivo}")
        self.resultado = "\n".join(lineas)

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
        rechazadas: lista de (gentefit_id, motivo)."""
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
                rechazadas.append((sesion["gentefit_id"],
                    f"plazas ocupadas ({sesion['plazas_ocupadas']}) mayor que el límite "
                    f"({sesion['plazas_limite']})"))
                continue

            # Regla 3: la fecha tiene que estar dentro del periodo del lote (incluidos los extremos)
            if not desde <= sesion["fecha"].date() <= hasta:
                rechazadas.append((sesion["gentefit_id"],
                    f"fecha {sesion['fecha']:%d/%m/%Y} fuera del periodo "
                    f"{desde:%d/%m/%Y} - {hasta:%d/%m/%Y}"))
                continue

            validas.append(sesion)

        return validas, rechazadas

    def _leer_sesion(self, nodo):
        """Convierte un <sesion> del XML en un diccionario con los nombres de campo del modelo."""
        return {
            "gentefit_id": nodo.findtext("gentefit_id"),
            # Hora de España tal cual viene. En el paso 5 la pasaremos a UTC.
            # [:19] = "AAAA-MM-DDTHH:MM:SS": ignora décimas de segundo o zona si las trajera
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
