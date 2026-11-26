{
    "name": "GenteFit - Intercambio de sesiones",
    "version": "17.0.1.0.0",
    "summary": "Importa y exporta lotes XML de sesiones con la app GenteFit",
    "author": "BugBusters (P59)",
    "category": "Services",
    "license": "LGPL-3",
    "depends": ["base"],
    "data": [
        "security/ir.model.access.csv",
        "views/gentefit_sesion_views.xml",
        "wizards/importar_lote_views.xml",
        "wizards/exportar_lote_views.xml",
    ],
    "application": True,
    "installable": True,
}