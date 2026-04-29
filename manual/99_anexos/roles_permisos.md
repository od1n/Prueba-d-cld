# Roles y permisos

> Detalle de qué puede hacer cada rol del sistema. La asignación se hace en *Sistema → Gestión de Usuarios* (sólo Admin).

## Los cuatro roles

El sistema maneja **cuatro roles base**. Cada usuario tiene **exactamente uno** (Admin es exclusivo y no se combina).

| Rol | Para qué se usa | Permisos clave |
|---|---|---|
| **Admin** | Administrador del sistema | Acceso total · Gestión de usuarios · Auditoría · Respaldos · Edición de tasas registradas |
| **Ventas** | Cajero / vendedor | Facturación · Clientes · Cuentas por cobrar · Consultar tasa |
| **Almacén** | Almacenista / encargado de planta | Inventario · Kardex · Compras · Proveedores · Producción · Moldes |
| **Contabilidad** | Contador / auxiliar contable | Reportes · Egresos · IVA · Cambio de precios · Editar tasa · Anular facturas |

> **Admin** tiene acceso implícito a **todos** los permisos sin necesidad de listarlos.

## Tabla detallada de permisos por rol

Verde ✓ = el rol tiene el permiso. Rojo ✗ = no lo tiene.

| Permiso / módulo | Admin | Ventas | Almacén | Contabilidad |
|---|:---:|:---:|:---:|:---:|
| **Facturas** (emitir, ver) | ✓ | ✓ | ✗ | ✓ |
| `FacturasAnular` (anular factura) | ✓ | ✗ | ✗ | ✓ |
| `NotasEntregaCrear` (crear NE) | ✓ | ✓ | ✗ | ✓ |
| Convertir NE a factura | ✓ | ✗ | ✗ | ✗ |
| Toggle visibilidad NE | ✓ | ✗ | ✗ | ✗ |
| **CuentasCobrar** | ✓ | ✓ | ✗ | ✗ |
| **Clientes** | ✓ | ✓ | ✗ | ✗ |
| **Reportes** (ver) | ✓ | ✓ | ✗ | ✓ |
| **Inventario** | ✓ | ✗ | ✓ | ✗ |
| **Kardex** | ✓ | ✗ | ✓ | ✗ |
| **Compras** | ✓ | ✗ | ✓ | ✓ |
| **Proveedores** | ✓ | ✗ | ✓ | ✗ |
| **IngresoProduccion** | ✓ | ✗ | ✓ | ✗ |
| **Moldes** | ✓ | ✗ | ✓ | ✗ |
| **Gastos** | ✓ | ✗ | ✗ | ✓ |
| **CambioPrecios** | ✓ | ✗ | ✗ | ✓ |
| `EgresosVer` (consultar egresos) | ✓ | ✓ | ✓ | ✓ |
| `EgresosRegistrar` (registrar) | ✓ | ✗ | ✓ | ✓ |
| `EgresosAnular` (anular egreso) | ✓ | ✗ | ✗ | ✓ |
| `EgresosCategorias` (CRUD cat.) | ✓ | ✗ | ✗ | ✓ |
| `TasaConsulta` (ver tasa) | ✓ | ✓ | ✓ | ✓ |
| `TasaIngreso` (cargar tasa nueva) | ✓ | ✓ | ✗ | ✓ |
| `TasaModificar` (editar registrada) | ✓ | ✗ | ✗ | ✓ |
| **Sistema** (acceso al menú) | ✓ | ✓ | ✓ | ✓ |
| **Respaldos** (gestionar) | ✓ | ✗ | ✗ | ✗ |
| **Log de auditoría** (ver) | ✓ | ✗ | ✗ | ✗ |
| **Gestión de Usuarios** | ✓ | ✗ | ✗ | ✗ |
| **Mi Perfil** (cambiar mis datos) | ✓ | ✓ | ✓ | ✓ |
| **Buscador global** (F3) | ✓ | ✓ | ✓ | ✓ |
| **Dashboard** (ver KPIs) | ✓ | ✓ | ✓ | ✓ |
| **Tarifas de bono producción** | ✓ | ✗ | ✗ | ✗ |
| **Nómina** | ✓ | ✗ | ✗ | ✗ |

## Notas importantes por rol

### Admin

- **Acceso total** — todo lo de la tabla está en ✓ implícitamente.
- Es el **único** que puede:
  - Crear/editar/desactivar usuarios.
  - Ver el log de auditoría.
  - Configurar respaldos.
  - Convertir notas de entrega a facturas legales.
  - Activar la "Vista NE" (mostrar notas de entrega en reportes).
  - Editar tarifas de bono de producción.
- La cuenta principal `admin` **no puede desactivarse**.

### Ventas

- Pensado para **cajeros y vendedores**.
- Puede **emitir facturas** (con IVA, exportación o nota de entrega) y **cobrar**.
- **No puede anular** facturas — eso requiere Contabilidad o Admin.
- Puede **consultar y cargar la tasa del día**, pero no editar una ya registrada.
- Puede ver **Reportes** (rentabilidad, libro IVA, etc.) — para acompañar la gestión comercial.

### Almacén

- Pensado para **almacenista, jefe de planta, supervisor de producción**.
- Maneja **inventario, kardex, compras, proveedores, producción, moldes**.
- **No emite facturas** ni atiende cuentas por cobrar.
- Puede **registrar egresos** (compras de materia prima) pero no anularlos.
- Puede **consultar la tasa**, pero no cargarla.

### Contabilidad

- Pensado para **contador o auxiliar contable**.
- Maneja **egresos, IVA, reportes financieros, cambio de precios**.
- Puede **anular facturas** (con motivo registrado en auditoría).
- Puede **editar la tasa registrada** (operación sensible).
- Puede **anular egresos** y **administrar categorías de gasto**.
- También tiene acceso a **Facturas** y **Compras** (para cierres contables).

## Cambio de rol

Para cambiar el rol de un usuario:

1. Ingresar como **Admin**.
2. *Sistema → Gestión de Usuarios*.
3. Doble clic en el usuario.
4. Cambiar las casillas de roles.
5. **Guardar Cambios**.

> **Atención**: Admin es exclusivo. Marcarlo desmarca y deshabilita los otros roles.

## Cuando un usuario no puede hacer algo

Si el usuario ve un mensaje del tipo `Tu rol (X) no tiene permiso para Y` o `No tienes permiso para Z`, hay dos opciones:

1. **No es su responsabilidad**: lo correcto es que otro rol lo haga.
2. **Es su responsabilidad pero falta el permiso**: el admin debe revisar el rol asignado o, si la operación cae en una zona gris, evaluar reasignar el rol.

## Pantallas relacionadas

- **`FrmUsuarios`** — para cambiar roles.
- **`FrmLogAuditoria`** — registra cada acción con el rol que la ejecutó.
