import uuid
from datetime import datetime, time, timedelta

import pytz

from odoo import api, fields, models
from odoo.exceptions import ValidationError

ZONA_ESPANA = pytz.timezone("Europe/Madrid")
HORA_APERTURA = 9
PLAZAS_MAXIMAS = 16
# Datos de la sesión: cambiarlos está sujeto a las reglas
CAMPOS_DE_NEGOCIO = {
    "fecha", "duracion", "estado",
    "actividad_ref", "actividad_nombre", "sala_ref", "sala_nombre",
    "entrenador_ref", "entrenador_nombre",
}


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

    # ------------------------------------------------------------------
    # Reglas del centro, las mismas que GenteFit. Los choques de sala y
    # entrenador los comprueba GenteFit, que conoce todas las sesiones.
    # Lo que llega del importador ya cumple las reglas: no se comprueba.
    # ------------------------------------------------------------------
    @api.constrains("fecha", "duracion", "estado")
    def _comprobar_horario_y_duracion(self):
        if self.env.context.get("gentefit_importando"):
            return
        for sesion in self:
            if sesion.estado == "Cancelada":
                continue
            if not 15 <= sesion.duracion <= 180:
                raise ValidationError("La duración debe estar entre 15 y 180 minutos.")
            # Odoo guarda en UTC: el horario se comprueba en hora de España
            inicio = pytz.utc.localize(sesion.fecha).astimezone(ZONA_ESPANA).replace(tzinfo=None)
            fin = inicio + timedelta(minutes=sesion.duracion)
            cierre = datetime.combine(inicio.date() + timedelta(days=1), time.min)
            if inicio.hour < HORA_APERTURA or fin > cierre:
                raise ValidationError(
                    "La sesión debe estar dentro del horario del centro (09:00 - 00:00).")

    @api.constrains("plazas_ocupadas", "plazas_limite")
    def _comprobar_plazas(self):
        if self.env.context.get("gentefit_importando"):
            return
        for sesion in self:
            if not 1 <= sesion.plazas_limite <= PLAZAS_MAXIMAS:
                raise ValidationError(f"El límite de plazas debe estar entre 1 y {PLAZAS_MAXIMAS}.")
            if not 0 <= sesion.plazas_ocupadas <= sesion.plazas_limite:
                raise ValidationError(
                    "Las plazas ocupadas deben estar entre 0 y el límite de plazas.")


    def _ahora(self):
        """Momento actual en UTC. Los tests lo fijan para no depender del día."""
        return fields.Datetime.now()

    @api.model_create_multi
    def create(self, vals_list):
        # Lo que llega del importador no hay que devolverlo a GenteFit
        if not self.env.context.get("gentefit_importando"):
            for vals in vals_list:
                fecha = fields.Datetime.to_datetime(vals.get("fecha"))
                if fecha and fecha <= self._ahora():
                    raise ValidationError("No se puede programar una sesión en el pasado.")
            vals_list = [dict(vals, pendiente_envio=True) for vals in vals_list]
        return super().create(vals_list)

    def write(self, vals):
        if not self.env.context.get("gentefit_importando"):
            if CAMPOS_DE_NEGOCIO & set(vals):
                self._comprobar_que_se_puede_modificar(vals)
            # Si quien escribe ya indica pendiente_envio (el exportador), se respeta
            if "pendiente_envio" not in vals:
                vals = dict(vals, pendiente_envio=True)
        return super().write(vals)

    def _comprobar_que_se_puede_modificar(self, vals):
        ahora = self._ahora()
        for sesion in self:
            if sesion.estado == "Cancelada":
                raise ValidationError("No se puede modificar una sesión cancelada.")
            if sesion.fecha <= ahora:
                raise ValidationError("No se puede modificar una sesión que ya ha empezado.")
        nueva = fields.Datetime.to_datetime(vals.get("fecha"))
        if nueva and nueva <= ahora:
            raise ValidationError("La nueva fecha y hora ya han pasado.")

    def copy(self, default=None):
        # La copia de una sesión cancelada es una sesión nueva, así que nace programada
        return super().copy(dict({"estado": "Programada"}, **(default or {})))