# `FrmLibroIVA` — Libro de Ventas y Compras IVA (SENIAT)

> Reporte fiscal mensual con el formato del **Libro de Ventas** y el **Libro de Compras** que pide SENIAT. Una pestaña para cada libro, con totales de base imponible, IVA y total.

## Cómo llegar

- **Ruta**: *Menú principal → Reportes → Libro IVA*.
- **Permiso requerido**: `Reportes` (o `Admin`).

## Para qué se usa

Generar la documentación fiscal mensual:

- **Libro de Ventas** — todas las facturas con IVA emitidas en el mes.
- **Libro de Compras** — todas las compras con IVA registradas en el mes.

Cada libro se puede exportar a **PDF** con el formato esperado por SENIAT.

> Las **notas de entrega** y las **facturas anuladas** **no aparecen** acá — son excluidas porque no son documentos fiscales declarables.

## Antes de empezar

- Tener facturas emitidas en *Transacciones → Facturas* con su IVA correctamente cargado.
- Tener compras registradas en *Transacciones → Egresos* (o legacy *Compras*) con base imponible y IVA.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────────────────┐
│ Libro de Ventas y Compras IVA                                          │
├────────────────────────────────────────────────────────────────────────┤
│ Mes: [Abril ▼]  Año: [2026 ▼]  [Consultar][PDF Ventas][PDF Compras]    │
├────────────────────────────────────────────────────────────────────────┤
│ ┌─Libro de Ventas─┬─Libro de Compras──────────────────────────────────┐│
│ │                                                                     ││
│ │ N° │Fecha │N°Factura│Cliente │Forma │Base Imp.│IVA   │Total          ││
│ │ 1  │02/04 │ 00125   │Pérez J │Cont. │ $300,00 │$48,00│$348,00        ││
│ │ 2  │03/04 │ 00126   │Marin M │Cred. │ $150,00 │$24,00│$174,00        ││
│ │                                                                     ││
│ │ Base Imponible: $4.250,00  IVA: $680,00  TOTAL: $4.930,00           ││
│ └─────────────────────────────────────────────────────────────────────┘│
│                                                            [ Cerrar ]  │
└────────────────────────────────────────────────────────────────────────┘
```

### Barra superior

| Control | Detalle |
|---|---|
| **Mes** | Enero a Diciembre. |
| **Año** | Últimos 5 años + actual. |
| **Consultar** | Carga los libros del mes/año. |
| **PDF Ventas** | Genera el PDF del Libro de Ventas. |
| **PDF Compras** | Genera el PDF del Libro de Compras. |

### Pestañas

| Pestaña | Color cabecera | Contenido |
|---|---|---|
| **Libro de Ventas** | Rojo oscuro | Facturas emitidas |
| **Libro de Compras** | Lila | Compras registradas |

### Columnas de Ventas

| Columna | Significado |
|---|---|
| **N°** | Número correlativo en el libro |
| **Fecha** | Fecha del documento |
| **N° Factura** | Número de la factura |
| **Cliente** | Razón social / nombre |
| **Forma Pago** | Contado, Crédito, etc. |
| **Base Imponible** | Subtotal sin IVA (USD) |
| **IVA (USD)** | Monto de IVA |
| **Total (USD)** | Total con IVA |

### Columnas de Compras

Igual que ventas, pero con `N° Compra` en vez de `N° Factura` y `Proveedor` en vez de `Cliente`.

### Pie con totales

Para cada libro, fondo del color de la pestaña:

```
Base Imponible: $X    IVA: $Y    TOTAL: $Z
```

## Casos de uso

### Caso 1 — Generar el Libro de Ventas del mes

1. Elegir el **Mes** y **Año**.
2. Hacer clic en **Consultar**.
3. Revisar la lista en la pestaña **Libro de Ventas**.
4. Verificar que los totales del pie cuadren con la contabilidad.
5. Hacer clic en **PDF Ventas**.
6. Elegir dónde guardar (nombre sugerido `LibroIVA_Ventas_<Mes><Anio>.pdf`).
7. **Guardar** — el PDF se abre automáticamente.

### Caso 2 — Generar el Libro de Compras del mes

Igual al Caso 1, pero clic en **PDF Compras** (nombre sugerido `LibroIVA_Compras_<Mes><Anio>.pdf`).

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `No hay datos de ventas para el periodo seleccionado.` | No hay facturas con IVA en ese mes/año | Verificar que las facturas estén cargadas y no estén anuladas |
| `No hay datos de compras para el periodo seleccionado.` | Sin compras con IVA en el mes | Idem |
| `PDF generado correctamente.` | OK | — |
| `Error al generar PDF: …` | Falla técnica | Reintentar; si persiste, llamar al administrador |

## Consejos

- **Las notas de entrega no figuran** — son entregas internas, no son ventas declarables. Para incluirlas hay que emitir la factura correspondiente en *Facturas*.
- **Las facturas anuladas se excluyen automáticamente**. Si una factura se anuló por error, hay que reactivarla con **Corregir** desde *Facturas*.
- **Los montos están en USD**. Si tu contador necesita el equivalente en bolívares, multiplicar por la **tasa BCV** del documento.
- Hay que generar el libro **antes del 15 de cada mes** (plazo SENIAT) para tener tiempo de declarar.

## Pantallas relacionadas

- **`FrmFacturas`** — origen de las facturas que aparecen en el Libro de Ventas.
- **`FrmEgresos`** (categoría Compras) o **`FrmCompras`** legacy — origen del Libro de Compras.
- **`DlgAnularFactura`** — anular una factura la saca del libro.
