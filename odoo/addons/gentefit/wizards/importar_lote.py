import base64

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
        sesiones = documento.findall("sesiones/sesion")
        # De momento solo contamos las sesiones. En el paso 4 vienen las reglas de negocio
        self.resultado = f"Fichero válido: {len(sesiones)} sesiones encontradas."
        return {
            "type": "ir.actions.act_window",
            "res_model": self._name,
            "res_id": self.id,
            "view_mode": "form",
            "target": "new",
        }

    def _leer_y_validar_xml(self):
        """Niveles A y B. Devuelve el XML ya leído o lanza UserError (lote rechazado entero)."""
        # Odoo guarda los Binary en base64: lo pasamos a los bytes originales del fichero
        contenido = base64.b64decode(self.fichero)

        # --- Nivel A: bien formado ---
        # Leemos bytes (no texto) para que lxml respete el encoding que declara el XML.
        # resolve_entities/no_network: por seguridad, no cargar nada externo (ataque XXE)
        parser = etree.XMLParser(resolve_entities=False, no_network=True)
        try:
            documento = etree.fromstring(contenido, parser)
        except etree.XMLSyntaxError as error:
            raise UserError(f"Lote rechazado: el fichero no es un XML bien formado.\n{error}")

        # --- Nivel B: válido según el XSD ---
        try:
            # parse() con la ruta del fichero: así encuentra tipos_comunes.xsd (el xs:include)
            esquema = etree.XMLSchema(etree.parse(RUTA_XSD_IDA))
        except (OSError, etree.XMLSchemaParseError) as error:
            raise UserError(f"No se ha podido cargar el XSD de ida ({RUTA_XSD_IDA}).\n{error}")

        if not esquema.validate(documento):
            errores = "\n".join(f"Línea {e.line}: {e.message}" for e in esquema.error_log)
            raise UserError(f"Lote rechazado: el fichero no cumple el XSD de ida.\n{errores}")

        return documento