# `FrmClientes` — Clientes

> Directorio de clientes del negocio. Permite alta, edición, eliminación y búsqueda — usado a diario por el personal de ventas.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Clientes*
- **Permiso requerido**: `Ventas` (o `Admin`)

## Para qué se usa

Mantener al día los datos de los clientes: razón social, CI/RIF, NIT, dirección, contacto, condiciones (crédito, descuento), tipo. Estos datos alimentan la facturación y los reportes fiscales (libro IVA).

## Qué ves en la pantalla

- **Cabecera azul oscuro**: título *"Clientes"*, botones **? Ayuda**, **+ Nuevo**, **Editar**, **Eliminar**.
- **Barra de búsqueda**: campo **Buscar** que filtra al escribir.
- **Tabla** con los clientes.
- **Pie**: resumen *"Clientes: N | Credito total: $X"* y botón **Cerrar**.

### Columnas

| Columna | Significado |
|---|---|
| **Codigo** | Código numérico (autogenerado desde 500) |
| **Nombre** | Razón social o nombre |
| **Ciudad**, **Estado** | Ubicación |
| **CiRif** | Cédula o RIF |
| **Telefono** | Contacto principal |
| **Credito** | Límite de crédito (USD) |
| **TipoCliente** | Regular · VIP · Mayorista · Minorista · Gobierno |
| **AFavor** | Saldo a favor del cliente (anticipos) |

> Doble clic en una fila abre el editor.

## Casos de uso

### Caso 1 — Crear un cliente nuevo

1. **+ Nuevo**.
2. El **Código** se genera automáticamente (siguiente número desde 500).
3. Llenar:
   - **Nombre** *(obligatorio, sólo letras/números/espacios)*.
   - **Dirección**, **Ciudad**, **Estado**.
   - **CI/RIF** y **NIT** (sin guiones).
   - **Fax**, **Teléfono**, **Teléfono2**.
   - **Crédito** (límite en USD), **% Descuento**, **A Favor** (saldo anticipado).
   - **Tipo Cliente**: `Regular`, `VIP`, `Mayorista`, `Minorista`, `Gobierno`.
   - **Fecha** (alta) y **Observaciones**.
4. **Guardar**.

### Caso 2 — Editar un cliente

1. Doble clic en la fila (o **Editar**).
2. Modificar (el **Código** queda bloqueado).
3. **Guardar**.

### Caso 3 — Eliminar un cliente

1. Seleccionar la fila.
2. **Eliminar**.
3. Confirmar `Eliminar cliente XXX — Nombre?`.

> ⚠ **Atención**: si el cliente tiene facturas o cuentas por cobrar registradas, conviene **no eliminarlo** — alterar su nombre o tipo es preferible para conservar la trazabilidad.

### Caso 4 — Buscar un cliente

Escribir en **Buscar** parte del código, nombre, ciudad, CI/RIF, teléfono o NIT. La búsqueda tolera ceros a la izquierda y separadores (`J-12345` encuentra `J12345`).

### Caso 5 — Ordenar la lista

Clic en el encabezado de **Codigo**, **Nombre**, **Credito**, **AFavor**, **CiRif** o **Ciudad** — segundo clic invierte el orden.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `El nombre es obligatorio.` | Faltó el nombre | Llenarlo |
| `El nombre solo puede contener letras, números y espacios.` | Caracteres no permitidos | Quitar símbolos |
| `El CI/RIF 'XXX' ya está registrado en el cliente N — Nombre.` | Duplicado de CI/RIF | Verificar si es el mismo cliente; usar el existente |

## Consejos

- El sistema **autogenera el código** (siguiente disponible desde 500). No se puede editar al crear.
- El campo **Tipo Cliente** sirve para reglas de descuento o filtrado en reportes.
- Para encontrar un cliente por número de teléfono, escribirlo completo o sin separadores en **Buscar**.
- **A Favor** se actualiza cuando el cliente paga más de lo que debe — útil para acreditar a futuras facturas.

## Pantallas relacionadas

- **`FrmFacturas`** — emite facturas a los clientes registrados aquí.
- **`FrmCuentasCobrar`** — saldos pendientes por cliente.
