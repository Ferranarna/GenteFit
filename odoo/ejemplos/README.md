# Ficheros de ejemplo del intercambio GenteFit ↔ Odoo

Esquemas en `../xsd/`. Cada fichero erróneo es una copia del válido con **un solo cambio**.

## Niveles de validación
- **A – Bien formado**: lo detecta el parser XML antes del XSD.
- **B – Válido**: lo detecta la validación contra el XSD.
- **C – Reglas de negocio**: pasa el XSD; lo debe detectar el código.

## Ida (GenteFit → Odoo) — los importa Odoo (Arnau)

| Fichero | Error | Nivel | Resultado esperado |
|---|---|---|---|
| ida_valido.xml | — | — | Se importan 2 sesiones |
| ida_err_mal_formado.xml | Falta `</duracion>` | A | Lote rechazado entero |
| ida_err_codificacion.xml | Guardado en Windows-1252 declarando UTF-8 | A | Lote rechazado entero |
| ida_err_texto_vacio.xml | Sala con solo espacios | B | Lote rechazado entero |
| ida_err_enum.xml | Estado "Pendiente" | B | Lote rechazado entero |
| ida_err_guid.xml | gentefit_id "1234" | B | Lote rechazado entero |
| ida_err_duplicado.xml | gentefit_id repetido | B | Lote rechazado entero |
| ida_err_sentido.xml | Sentido de vuelta | B | Lote rechazado entero |
| ida_err_total.xml | total_registros = 3 con 2 sesiones | C | Lote rechazado entero |
| ida_err_plazas.xml | 17 ocupadas de 16 | C | Sesión 1 rechazada, sesión 2 importada |
| ida_err_fuera_periodo.xml | Sesión 2 fuera del periodo | C | Sesión 2 rechazada, sesión 1 importada |

## Vuelta (Odoo → GenteFit) — los importa GenteFit (Ferran)

| Fichero | Error | Nivel | Resultado esperado |
|---|---|---|---|
| vuelta_valido.xml | — | — | 1 modificada, 1 cancelada, 1 creada |
| vuelta_err_duracion.xml | Duración 200 | B | Lote rechazado entero |
| vuelta_err_ref_inexistente.xml | Actividad que no existe en GenteFit | C | Sesión nueva rechazada, las otras 2 aplicadas |
| vuelta_err_solapamiento.xml | Sala ocupada a esa hora | C | Sesión nueva rechazada, las otras 2 aplicadas |

Nota: los GUID de los ejemplos son inventados. Los tests deben crear antes en la BD
la actividad, sala, entrenador y sesiones con esos mismos Id.