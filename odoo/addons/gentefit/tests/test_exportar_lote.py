import base64
import os
import uuid
from datetime import datetime

import pytz
from lxml import etree

from odoo.exceptions import UserError
from odoo.tests import TransactionCase, tagged

CARPETA_EJEMPLOS = os.path.join(os.path.dirname(__file__), "ejemplos")
RUTA_XSD_VUELTA = "/mnt/gentefit-xsd/lote_sesiones_vuelta.xsd"
ZONA_ESPANA = pytz.timezone("Europe/Madrid")


@tagged("post_install", "-at_install")
class TestExportarLote(TransactionCase):

    def setUp(self):
        super().setUp()
        self.Sesion = self.env["gentefit.sesion"]
        self.Sesion.search([]).unlink()

    # ---------- Ayudas ----------

    def _crear(self, fecha_local, contexto=None, **cambios):
        """Crea una sesión indicando la hora de España."""
        fecha_utc = ZONA_ESPANA.localize(
            datetime.fromisoformat(fecha_local)).astimezone(pytz.utc).replace(tzinfo=None)
        valores = {
            "fecha": fecha_utc,
            "duracion": 60,
            "actividad_ref": str(uuid.uuid4()),
            "actividad_nombre": "Pilates",
            "sala_ref": str(uuid.uuid4()),
            "sala_nombre": "Sala 1",
            "entrenador_ref": str(uuid.uuid4()),
            "entrenador_nombre": "Laura",
        }
        valores.update(cambios)
        return self.Sesion.with_context(**(contexto or {})).create(valores)

    def _asistente(self, desde="2026-11-30", hasta="2026-12-06"):
        return self.env["gentefit.exportar.lote"].create({
            "periodo_desde": desde,
            "periodo_hasta": hasta,
        })

    def _exportar(self, desde="2026-11-30", hasta="2026-12-06"):
        asistente = self._asistente(desde, hasta)
        asistente.action_exportar()
        return etree.fromstring(base64.b64decode(asistente.fichero))

    @staticmethod
    def _datos(sesion):
        """Contenido de un <sesion> como lista comparable."""
        return [(nodo.tag, nodo.text.strip(), dict(nodo.attrib))
                for nodo in sesion if isinstance(nodo.tag, str)]

    # ---------- El fichero ----------

    def test_el_xml_cumple_el_xsd(self):
        self._crear("2026-11-30T19:00:00")
        documento = self._exportar()
        esquema = etree.XMLSchema(etree.parse(RUTA_XSD_VUELTA))
        self.assertTrue(esquema.validate(documento), esquema.error_log)

    def test_coincide_con_vuelta_valido(self):
        ejemplo = etree.parse(os.path.join(CARPETA_EJEMPLOS, "vuelta_valido.xml")).getroot()
        for nodo in ejemplo.findall("sesiones/sesion"):
            self._crear(
                nodo.findtext("fecha"),
                gentefit_id=nodo.findtext("gentefit_id"),
                duracion=int(nodo.findtext("duracion")),
                estado=nodo.findtext("estado"),
                actividad_ref=nodo.find("actividad").get("id"),
                actividad_nombre=nodo.findtext("actividad"),
                sala_ref=nodo.find("sala").get("id"),
                sala_nombre=nodo.findtext("sala"),
                entrenador_ref=nodo.find("entrenador").get("id"),
                entrenador_nombre=nodo.findtext("entrenador"),
            )

        documento = self._exportar()

        for campo in ("sentido", "periodo_desde", "periodo_hasta", "total_registros"):
            self.assertEqual(documento.findtext(f"cabecera/{campo}"),
                             ejemplo.findtext(f"cabecera/{campo}"))
        self.assertEqual(
            [self._datos(s) for s in documento.findall("sesiones/sesion")],
            [self._datos(s) for s in ejemplo.findall("sesiones/sesion")])

    # ---------- GUID y horas ----------

    def test_una_sesion_nueva_nace_con_guid(self):
        sesion = self._crear("2026-11-30T19:00:00")
        self.assertEqual(str(uuid.UUID(sesion.gentefit_id)), sesion.gentefit_id)
        self.assertTrue(sesion.pendiente_envio)

    def test_la_hora_se_convierte_en_invierno_y_en_verano(self):
        self._crear("2026-11-30T19:00:00")
        self._crear("2026-07-01T19:00:00")
        invierno = self._exportar()
        verano = self._exportar("2026-07-01", "2026-07-01")
        self.assertEqual(invierno.findtext("sesiones/sesion/fecha"), "2026-11-30T19:00:00")
        self.assertEqual(verano.findtext("sesiones/sesion/fecha"), "2026-07-01T19:00:00")

    # ---------- Qué se exporta ----------

    def test_solo_exporta_las_pendientes_del_periodo(self):
        dentro = self._crear("2026-12-06T23:00:00")
        self._crear("2026-12-07T00:00:00")                                   # fuera
        self._crear("2026-12-01T10:00:00", {"gentefit_importando": True})    # no pendiente
        documento = self._exportar()
        self.assertEqual(
            [s.findtext("gentefit_id") for s in documento.findall("sesiones/sesion")],
            [dentro.gentefit_id])

    def test_despues_de_exportar_se_desmarca(self):
        sesion = self._crear("2026-11-30T19:00:00")
        self._exportar()
        self.assertFalse(sesion.pendiente_envio)

    def test_el_numero_de_lote_es_correlativo(self):
        self._crear("2026-11-30T19:00:00")
        primero = self._exportar().findtext("cabecera/id_lote")
        self._crear("2026-12-01T19:00:00")
        segundo = self._exportar().findtext("cabecera/id_lote")
        self.assertEqual(int(segundo[-3:]), int(primero[-3:]) + 1)

    # ---------- Errores ----------

    def test_periodo_sin_sesiones_no_genera_fichero(self):
        asistente = self._asistente()
        with self.assertRaises(UserError):
            asistente.action_exportar()
        self.assertFalse(asistente.fichero)

    def test_periodo_al_reves(self):
        with self.assertRaises(UserError):
            self._asistente("2026-12-06", "2026-11-30").action_exportar()

    def test_un_dato_no_valido_no_genera_ni_desmarca(self):
        sesion = self._crear("2026-11-30T19:00:00", actividad_ref="1234")
        with self.assertRaises(UserError):
            self._asistente().action_exportar()
        self.assertTrue(sesion.pendiente_envio)