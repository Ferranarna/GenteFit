from odoo import fields, models


class GentefitImportarLote(models.TransientModel):
    # TransientModel: ventana de usar y tirar (Odoo borra estos registros solo)
    _name = "gentefit.importar.lote"
    _description = "Asistente para importar un lote de sesiones de GenteFit"

    # El fichero XML que sube el usuario (Odoo lo guarda codificado en base64)
    fichero = fields.Binary(string="Fichero XML", required=True)
    # Nombre del archivo subido (p. ej. GF-2026-11-25-001.xml)
    nombre_fichero = fields.Char(string="Nombre del fichero")
    # Aquí escribiremos el informe final: creadas, actualizadas y rechazadas
    resultado = fields.Text(string="Resultado", readonly=True)

    def action_importar(self):
        # De momento solo escribe un texto. En los pasos 3 a 6 aquí irá la importación de verdad
        self.resultado = "Todavía no implementado"
        # Vuelve a abrir la misma ventana para que se vea el resultado
        return {
            "type": "ir.actions.act_window",
            "res_model": self._name,
            "res_id": self.id,
            "view_mode": "form",
            "target": "new",
        }


    