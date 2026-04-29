# `FrmProveedores` — Proveedores

> Directorio de proveedores. Mantiene los datos de contacto, condiciones (días de crédito, % retención) y tipo de cada proveedor.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Proveedores*
- **Permiso requerido**: `Contabilidad` (o `Admin`). Los botones **+ Nuevo** y **Eliminar** sólo se ven con rol `Admin`.

## Para qué se usa

Llevar el directorio de a quién le compramos: información fiscal, contactos, condiciones comerciales (días de crédito, retención de IVA o ISLR), tipo de proveedor. Es la base que usan **Egresos**, **Cuentas por Pagar** e **Insumos por Proveedor**.

## Qué ves en la pantalla

- **Cabecera azul oscuro**: título *"Proveedores"*, botones **? Ayuda**, **+ Nuevo**, **Editar**, **Eliminar**.
- **Barra de búsqueda** con campo **Buscar**.
- **Tabla** central con todos los proveedores.
- **Pie**: contador *"Proveedores: N"* y botón **Cerrar**.

### Columnas

| Columna | Significado |
|---|---|
| **Codigo** | Código numérico (autogenerado) |
| **Nombre** | Razón social del proveedor |
| **Representante** | Persona de contacto |
| **Telefono1** | Teléfono principal |
| **CiRif** | Cédula o RIF |
| **Nit** | NIT |
| **DiasCredito** | Plazo habitual en días |
| **Retencion** | % de retención al pagarle |
| **Tipo** | Nacional · Internacional · Servicio · Mixto |

> Doble clic en una fila abre el editor.

## Casos de uso

### Caso 1 — Alta de un proveedor *(Admin)*

1. **+ Nuevo**.
2. El **Código** se autogenera.
3. Llenar **Nombre**, **Representante**, **Dirección**, **Teléfono 1/2**, **Fax 1/2**, **CI/RIF**, **NIT**.
4. **Días Crédito** (0–365), **% Retención** (0–100).
5. **Tipo**: `Nacional`, `Internacional`, `Servicio`, `Mixto`.
6. **Fecha Inicio** y **Observaciones**.
7. **Guardar**.

### Caso 2 — Editar un proveedor

1. Doble clic (o **Editar**).
2. Modificar (el **Código** queda bloqueado).
3. **Guardar**.

### Caso 3 — Eliminar un proveedor *(Admin)*

1. Seleccionar la fila.
2. **Eliminar**.
3. Confirmar `Eliminar proveedor N — Nombre?`.

> ⚠ **Antes de eliminar**: verificar que no tenga insumos asociados ni cuentas por pagar abiertas.

### Caso 4 — Buscar

Escribir en **Buscar** parte del código, nombre, CI/RIF, NIT, teléfono o representante. Tolera ceros a la izquierda.

## Validaciones y errores frecuentes

Las validaciones son similares a *Clientes*: nombre obligatorio, sólo letras/números/espacios, CI/RIF único.

## Consejos

- Si trabajás con proveedores extranjeros, marcar **Tipo: Internacional** — sirve para filtros y reportes.
- **Días Crédito** y **% Retención** se usan al cargar cuentas por pagar y aplicar retenciones.

## Pantallas relacionadas

- **`FrmProductosProveedor`** — insumos y materias primas asociados a cada proveedor.
- **`FrmCuentasPagar`** — saldos pendientes por proveedor.
- **`FrmEgresos`** — registro de pagos a proveedores.
