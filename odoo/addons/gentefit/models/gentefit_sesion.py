from odoo import fields, models


class GentefitSesion(models.Model):
    _name = "gentefit.sesion"
    _description = "Sesión de GenteFit"
    _order = "fecha"
    _rec_name = "actividad_nombre"

    # Referencia externa: el SesionId (GUID) de GenteFit
    gentefit_id = fields.Char(string="Id GenteFit", required=True, index=True, copy=False)

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

    _sql_constraints = [
        ("gentefit_id_unico", "unique(gentefit_id)",
         "Ya existe una sesión con ese Id de GenteFit."),
    ]