import uuid

from odoo import api, fields, models


class GentefitSesion(models.Model):
    _name = "gentefit.sesion"
    _description = "Sesión de GenteFit"
    _order = "fecha"
    _rec_name = "actividad_nombre"

    # Referencia externa: el SesionId (GUID) de GenteFit.
    # Una sesión creada a mano en Odoo nace ya con su GUID.
    gentefit_id = fields.Char(
        string="Id GenteFit", required=True, index=True, copy=False,
        default=lambda self: str(uuid.uuid4()),
    )

    fecha = fields.Datetime(string="Fecha y hora", required=True)
    duracion = fields.Integer(string="Duración (min)", required=True, default=60)
    estado = fields.Selection(
        [("Programada", "Programada"), ("Cancelada", "Cancelada")],
        string="Estado", required=True, default="Programada",
    )

    # Referencias a GenteFit: Id + nombre (el Id sirve para la vuelta)
    actividad_ref = fields.Char(string="Id actividad", required=True)
    actividad_nombre = fields.Char(string="Actividad", required=True)
    intensidad = fields.Selection(
        [("Alta", "Alta"), ("Media", "Media"), ("Baja", "Baja")],
        string="Intensidad",
    )
    sala_ref = fields.Char(string="Id sala", required=True)
    sala_nombre = fields.Char(string="Sala", required=True)
    entrenador_ref = fields.Char(string="Id entrenador", required=True)
    entrenador_nombre = fields.Char(string="Entrenador", required=True)

    plazas_ocupadas = fields.Integer(string="Plazas ocupadas", default=0)
    plazas_limite = fields.Integer(string="Plazas límite", default=16)

    # Creada o modificada en Odoo y todavía sin exportar a GenteFit
    pendiente_envio = fields.Boolean(string="Pendiente de enviar", default=False, copy=False)

    _sql_constraints = [
        ("gentefit_id_unico", "unique(gentefit_id)",
         "Ya existe una sesión con ese Id de GenteFit."),
    ]

    @api.model_create_multi
    def create(self, vals_list):
        # Lo que llega del importador no hay que devolverlo a GenteFit
        if not self.env.context.get("gentefit_importando"):
            vals_list = [dict(vals, pendiente_envio=True) for vals in vals_list]
        return super().create(vals_list)

    def write(self, vals):
        # Si quien escribe ya indica pendiente_envio (el exportador), se respeta
        if not self.env.context.get("gentefit_importando") and "pendiente_envio" not in vals:
            vals = dict(vals, pendiente_envio=True)
        return super().write(vals)