# Manual de uso — Sistema de Gestión

Manual operativo para usuarios finales del sistema.

## Cómo está organizado este manual

El manual está dividido en **fichas por pantalla**, agrupadas por módulo. Cada ficha responde a tres preguntas:

1. **¿Para qué sirve esta pantalla?** (propósito)
2. **¿Qué hago aquí?** (casos de uso paso a paso)
3. **¿Qué hago si algo falla?** (errores comunes y validaciones)

## Estructura de archivos

```
manual/
├── README.md                  ← este archivo
├── PLANTILLA.md               ← formato estándar de cada ficha
├── INVENTARIO.md              ← lista maestra con enlaces a todas las pantallas
├── 00_introduccion.md         ← capítulo: arranque, login, navegación
├── 00_introduccion/           ← Login, Cambio de contraseña, Alertas, Buscador
├── 01_dashboard/              ← Dashboard V2 (activo) y Dashboard legacy
├── 02_transacciones/          ← Facturas, CxC, CxP, Egresos, Precios, editores
├── 03_archivos/               ← Inventario, Clientes, Proveedores, Moldes, Kardex…
├── 04_produccion/             ← Registro diario, Órdenes, Mermas, Ingreso legacy
├── 05_nomina/                 ← Empleados, Procesar, Préstamos, LOTTT
├── 06_reportes/               ← Rentabilidad, Flujo de caja, Libro IVA, Comparación, PE
├── 07_tasa_cambio/            ← Actualizar tasa, Trazabilidad
├── 08_sistema/                ← Respaldos, Perfil, Auditoría, Usuarios
└── 99_anexos/                 ← Roles y permisos, atajos, glosario, exportar PDF
```

## Por dónde empezar

- **Si nunca usaste el sistema:** [`00_introduccion.md`](00_introduccion.md)
- **Si querés ver la lista completa de pantallas:** [`INVENTARIO.md`](INVENTARIO.md)
- **Si vas a documentar una nueva pantalla:** [`PLANTILLA.md`](PLANTILLA.md)
- **Si vas a generar el PDF final:** [`99_anexos/exportar_pdf.md`](99_anexos/exportar_pdf.md)

## Audiencia

Este manual está dirigido al **usuario final operativo** (cajero, almacenista, auxiliar contable, supervisor de planta). No es un manual técnico — no cubre instalación, base de datos ni código.

## Convenciones

| Símbolo | Significado |
|---|---|
| **Negrita** | Nombre exacto de un botón, campo o pantalla |
| `Código` | Texto literal a escribir o ruta de archivo |
| → | Secuencia de pasos: *Menú → Transacciones → Facturas* |
| ⚠ | Aviso importante o validación |
| 🔒 | Acción que requiere rol Admin |

## Cómo exportar este manual a PDF

Ver `99_anexos/exportar_pdf.md`.
