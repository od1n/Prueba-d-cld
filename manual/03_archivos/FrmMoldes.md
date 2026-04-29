# `FrmMoldes` — Catálogo de Moldes

> Lista los **moldes de producción**, qué productos genera cada uno (primera, segunda, reciclado), días desde el último mantenimiento y permite acceder a `FrmMantenimientoMoldes` y a la configuración de productos del molde.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Moldes*
- **Permiso requerido**: `Almacén` (o `Admin`)

## Para qué se usa

Mantener el catálogo de moldes y su asociación con productos terminados. Cada molde puede generar tres tipos de salida:

- **Primera**: producto de calidad estándar (material virgen).
- **Segunda**: producto de menor calidad / segunda selección (material virgen).
- **Reciclado**: producto fabricado con material reciclado.

Usado por almacén/producción para saber qué se fabrica con cada molde y planificar mantenimientos.

## Qué ves en la pantalla

- **Cabecera azul-cian**: título *"Tabla de Moldes"*, botones **+ Nuevo**, **Productos Molde**, **Mantenimiento**, **Editar**, **Eliminar**.
- **Tabla** con todos los moldes. Las filas se colorean:
  - **Rojo claro** → más de 180 días sin mantenimiento
  - **Amarillo claro** → entre 90 y 180 días sin mantenimiento
- **Pie**: resumen *"Moldes: N | Mant. vencido (>180 dias): X | Sin productos configurados: Y"* y botón **Cerrar**.

### Columnas

| Columna | Significado |
|---|---|
| **CodigoMold** | Código del molde |
| **Descripcion** | Nombre del molde |
| **Productos** | `1a:CODIGO 2a:CODIGO R:CODIGO` o `⚠ Sin configurar` |
| **UltMant** | Fecha del último mantenimiento |
| **DiasDesdeUltMant** | Días transcurridos |
| **DescMant** | Descripción del último mantenimiento |
| **MatXUnid** | Material por unidad fabricada |

## Casos de uso

### Caso 1 — Crear un molde

1. **+ Nuevo**.
2. Llenar **CodigoMold** (único), **Descripción**, **CodigoProducto** principal, **CantMatXunid**, etc.
3. **Guardar**.

> Si el código ya existe, aparece `El codigo 'XXX' ya existe.`

### Caso 2 — Configurar los productos del molde

**Cuándo**: hay que decirle al sistema qué código de inventario corresponde a *Primera*, *Segunda* y *Reciclado*.

1. Seleccionar el molde.
2. Hacer clic en **Productos Molde**.
3. En el diálogo **Productos del Molde: XXX**, en cada desplegable elegir el producto del catálogo o dejar `-- No aplica --`:
   - **Material VIRGEN**: Producto Primera, Producto Segunda.
   - **Material RECICLADO**: Producto Reciclado.
4. **Guardar**.

**Resultado**: aparece `Productos del molde XXX guardados correctamente.`

> Si no se selecciona ninguno, el sistema marca el molde como `⚠ Sin configurar` en la lista.

### Caso 3 — Ir al programa de mantenimientos

1. Hacer clic en **Mantenimiento** (cabecera).
2. Se abre `FrmMantenimientoMoldes`.

### Caso 4 — Editar un molde

1. Doble clic (o **Editar**).
2. Modificar y **Guardar**.

### Caso 5 — Eliminar un molde

1. Seleccionar.
2. **Eliminar**.
3. Confirmar `Eliminar molde XXX?`.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `El codigo 'XXX' ya existe.` | Código duplicado al crear | Usar otro código |
| `Selecciona un molde primero.` | Acción que requiere fila seleccionada | Seleccionar antes |

## Consejos

- Mantener todos los moldes con productos configurados — los que aparecen `⚠ Sin configurar` no permiten registrar producción correctamente.
- Las filas **rojas** (>180 días sin mantenimiento) deberían ser prioridad: ir a **Mantenimiento** y registrar uno.
- Clic en encabezados ordena por **CodigoMold**, **Descripcion**, **Productos**, **UltMant** o **DiasDesdeUltMant**.

## Pantallas relacionadas

- **`FrmMantenimientoMoldes`** — programa de mantenimientos e historial.
- **`FrmRegistroProduccionDia`** / **`FrmIngresoProduccion`** — usan los moldes al registrar producción.
- **`FrmInventario`** — productos terminados que se asocian con cada molde.
