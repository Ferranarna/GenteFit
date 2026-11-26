"""Tests del asistente "Importar lote" (GenteFit -> Odoo).

Cada fichero de tests/ejemplos (copia de odoo/ejemplos) tiene que dar
el resultado que indica odoo/ejemplos/README.md.

Ejecutar (desde la carpeta odoo):
docker compose exec odoo odoo -d gentefit -u gentefit --test-enable --test-tags /gentefit
    --db_host db --db_user odoo --db_password odoo --stop-after-init --no-http
"""
import base64
import os
from datetime import datetime

from odoo.exceptions import UserError
from odoo.tests import TransactionCase, tagged

CARPETA_EJEMPLOS = os.path.join(os.path.dirname(__file__), "ejemplos")

# Los GUID de las dos sesiones de los ejemplos
PILATES = "6b1f2c3a-9d4e-4f7a-8b21-0c5d7e9f1a23"
SPINNING = "f02d8a6e-3c1b-4e9f-a571-2b8d4c6e0f99"



@tagged("post_install", "-at_install")
class TestImportarLote(TransactionCase):


    def setUp(self):
        super().setUp()
        self.Sesion = self.env["gentefit.sesion"]

        self.Sesion.search([]).unlink()

    # ---------- Ayudas ----------

    def _asistente(self, nombre_fichero):
        """Crea el asistente con el fichero de ejemplo ya 'subido'."""
        with open(os.path.join(CARPETA_EJEMPLOS, nombre_fichero), "rb") as f:
            contenido = f.read()
        return self.env["gentefit.importar.lote"].create({
            "fichero": base64.b64encode(contenido),
            "nombre_fichero": nombre_fichero,
        })

    def _importar(self, nombre_fichero):
        asistente = self._asistente(nombre_fichero)
        asistente.action_importar()
        return asistente.resultado

    def _ids_guardados(self):
        return set(self.Sesion.search([]).mapped("gentefit_id"))

    # ---------- Fichero válido ----------

    def test_valido_crea_dos_sesiones(self):
        resultado = self._importar("ida_valido.xml")

        self.assertIn("Creadas: 2", resultado)
        self.assertIn("Rechazadas: 0", resultado)
        self.assertEqual(self._ids_guardados(), {PILATES, SPINNING})

        pilates = self.Sesion.search([("gentefit_id", "=", PILATES)])
        self.assertEqual(pilates.actividad_nombre, "Pilates Iniciación")
        self.assertEqual(pilates.actividad_ref, "a3c9e1f0-2b7d-4c58-9e16-7f4a2d8b0c11")
        self.assertEqual(pilates.sala_nombre, "Sala Montaña")
        self.assertEqual(pilates.entrenador_nombre, "Laura O'Connor")
        self.assertEqual(pilates.estado, "Programada")
        self.assertEqual(pilates.intensidad, "Baja")
        self.assertEqual(pilates.duracion, 60)
        self.assertEqual(pilates.plazas_ocupadas, 12)
        self.assertEqual(pilates.plazas_limite, 16)

    def test_fecha_se_guarda_en_utc(self):
        self._importar("ida_valido.xml")
        pilates = self.Sesion.search([("gentefit_id", "=", PILATES)])
        # 30/11 19:00 en España (invierno, UTC+1) = 18:00 UTC
        self.assertEqual(pilates.fecha, datetime(2026, 11, 30, 18, 0))

    def test_hora_de_verano_resta_dos_horas(self):
        utc = self.env["gentefit.importar.lote"]._hora_espana_a_utc(datetime(2026, 7, 1, 19, 0))
        self.assertEqual(utc, datetime(2026, 7, 1, 17, 0))

    def test_importar_dos_veces_actualiza_sin_duplicar(self):
        self._importar("ida_valido.xml")
        resultado = self._importar("ida_valido.xml")

        self.assertIn("Creadas: 0", resultado)
        self.assertIn("Actualizadas: 2", resultado)
        self.assertEqual(self.Sesion.search_count([]), 2)

    def test_actualizar_cambia_los_datos(self):
        self._importar("ida_valido.xml")
        pilates = self.Sesion.search([("gentefit_id", "=", PILATES)])
        pilates.plazas_ocupadas = 1  
        self._importar("ida_valido.xml")  
        self.assertEqual(pilates.plazas_ocupadas, 12)

    # ---------- Niveles A y B: lote rechazado entero ----------

    def test_niveles_a_y_b_rechazan_el_lote_entero(self):
        casos = {
            "ida_err_mal_formado.xml": "bien formado",
            "ida_err_codificacion.xml": "bien formado",
            "ida_err_texto_vacio.xml": "XSD",
            "ida_err_enum.xml": "XSD",
            "ida_err_guid.xml": "XSD",
            "ida_err_duplicado.xml": "XSD",
            "ida_err_sentido.xml": "XSD",
        }
        for fichero, texto_esperado in casos.items():
            with self.subTest(fichero=fichero):
                with self.assertRaises(UserError) as error:
                    self._importar(fichero)
                self.assertIn("Lote rechazado", str(error.exception))
                self.assertIn(texto_esperado, str(error.exception))
                self.assertEqual(self.Sesion.search_count([]), 0)

    # ---------- Nivel C ----------

    def test_total_incorrecto_rechaza_el_lote_entero(self):
        with self.assertRaises(UserError) as error:
            self._importar("ida_err_total.xml")
        self.assertIn("la cabecera dice 3 sesiones", str(error.exception))
        self.assertEqual(self.Sesion.search_count([]), 0)

    def test_plazas_rechaza_solo_esa_sesion(self):
        resultado = self._importar("ida_err_plazas.xml")

        self.assertIn("Creadas: 1", resultado)
        self.assertIn("Rechazadas: 1", resultado)
        self.assertIn("plazas ocupadas (17)", resultado)
        self.assertEqual(self._ids_guardados(), {SPINNING})

    def test_fuera_de_periodo_rechaza_solo_esa_sesion(self):
        resultado = self._importar("ida_err_fuera_periodo.xml")

        self.assertIn("Creadas: 1", resultado)
        self.assertIn("Rechazadas: 1", resultado)
        self.assertIn("fuera del periodo", resultado)
        self.assertEqual(self._ids_guardados(), {PILATES})

    # ---------- Contexto para la exportación ----------

    def test_importar_no_marca_pendiente_de_envio(self):
        self._importar("ida_valido.xml")
        sesiones = self.Sesion.search([])

        if "pendiente_envio" in self.Sesion._fields:
            self.assertFalse(any(sesiones.mapped("pendiente_envio")))
