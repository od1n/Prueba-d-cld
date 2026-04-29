# `FrmSistema` — Configuración del sistema

> Ventana central del módulo Sistema. Funciona como **lanzador** de las opciones administrativas: respaldos, perfil, log de auditoría, gestión de usuarios. Muestra también la versión del esquema de base de datos.

## Cómo llegar

- **Ruta**: *Menú principal → barra inferior → ⚙ Sistema*.
- **Atajo**: ninguno.
- **Permiso requerido**: ninguno para abrirla. Las opciones avanzadas (log, usuarios) **sólo aparecen si sos `Admin`**.

## Para qué se usa

Concentra los accesos a las funciones de administración del sistema:

- **Respaldos** — crear/restaurar/verificar copias de seguridad.
- **Perfil** — cambiar contraseña y preferencias del usuario actual.
- **Log de auditoría** *(sólo Admin)* — visor del registro de acciones.
- **Gestión de usuarios** *(sólo Admin)* — crear, editar y desactivar usuarios.

También muestra la **versión del esquema** de la base de datos (informativo, útil al reportar errores).

## Antes de empezar

- Para acceder a *Log de auditoría* y *Gestión de Usuarios* necesitás rol Admin.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────┐
│ Configuracion del Sistema                                │  ← banner azul
├──────────────────────────────────────────────────────────┤
│  Nombre del sistema:  [ Sistema de Gestion         ]     │
│  Version:             [ 1.0.0                      ]     │
│  Empresa:             [ Mi Empresa S.A.            ]     │
│  Moneda:              [ MXN - Peso Mexicano   ▼   ]      │
│                                                          │
│  [ Ver respaldos ]                                       │
│  Base de datos: esquema v23 (codigo: v23) — Al dia       │  ← versión BD
│                                                          │
│  [ Mi Perfil y preferencias ]                            │
│                                                          │
│  [ Ver log de auditoria ]      (sólo Admin)              │
│  [ Gestión de Usuarios ]       (sólo Admin)              │
│                                                          │
│  [ Guardar cambios ]   [ Cerrar ]                        │
└──────────────────────────────────────────────────────────┘
```

### Campos informativos (no editables operativamente)

| Campo | Para qué sirve |
|---|---|
| **Nombre del sistema** | Etiqueta del sistema (informativo). |
| **Versión** | Versión de la aplicación. |
| **Empresa** | Nombre de la empresa que opera el sistema. |
| **Moneda** | Moneda principal (informativo). El sistema maneja Bs/USD por separado a través de la tasa BCV. |

> ⚠ **Estos campos son visuales — al pulsar Guardar cambios aparece "Configuracion guardada" pero no se modifican parámetros operativos.** El cambio real de moneda y empresa se hace en otros módulos.

### Botones de acción

| Botón | Para qué sirve |
|---|---|
| **Ver respaldos** | Abre `FrmRespaldos` (gestión de respaldos). |
| **Mi Perfil y preferencias** | Abre `FrmPerfil` con tus datos, contraseña y preferencias. |
| **Ver log de auditoria** *(Admin)* | Abre `FrmLogAuditoria` con el visor de eventos del sistema. |
| **Gestión de Usuarios** *(Admin)* | Abre `FrmUsuarios` para administrar usuarios y roles. |
| **Guardar cambios** | Muestra el aviso `Configuracion guardada.` (decorativo). |
| **Cerrar** | Cierra la ventana. |

### Indicador de versión de base de datos

Línea en cursiva con el formato:

`Base de datos: esquema vN (codigo: vM) — Al dia`

- **N** = versión actual del esquema en la base de datos.
- **M** = versión que el código espera.
- Si **N == M** dice `— Al dia` en verde.
- Si **N < M** dice `— PENDIENTE MIGRACION` en rojo (avisar al administrador).

## Casos de uso

### Caso 1 — Acceder a respaldos

**Pasos**:

1. Abrir **Sistema** desde la barra inferior del menú.
2. Hacer clic en **Ver respaldos**.

**Resultado**: se abre `FrmRespaldos` (ver su ficha).

### Caso 2 — Cambiar mi contraseña o preferencias

**Pasos**:

1. **Sistema → Mi Perfil y preferencias**.

**Resultado**: se abre `FrmPerfil`.

### Caso 3 — Revisar el log de auditoría (Admin)

**Pasos**:

1. **Sistema → Ver log de auditoria**.

**Resultado**: se abre `FrmLogAuditoria` con el log filtrable por fecha/usuario/acción.

### Caso 4 — Gestionar usuarios (Admin)

**Pasos**:

1. **Sistema → Gestión de Usuarios**.

**Resultado**: se abre `FrmUsuarios` para crear/editar/desactivar.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Configuracion guardada.` | Tras pulsar **Guardar cambios** | Es informativo (decorativo). |
| `No se pudo leer version de esquema` | Falla al consultar la BD | Llamar al administrador — la BD podría estar en un estado inconsistente. |
| `Base de datos: esquema vN (codigo: vM) — PENDIENTE MIGRACION` | El código es más nuevo que la BD | Actualizar el sistema o pedir al admin que ejecute la migración. |

## Consejos

- Esta pantalla es un **lanzador**. Las funcionalidades reales viven en las pantallas que abre.
- Si sos usuario operativo (Ventas, Almacén, Contabilidad) sólo verás **Ver respaldos**, **Mi Perfil y preferencias** y los botones decorativos. Los botones de auditoría y usuarios sólo aparecen para Admin.
- **El indicador de esquema** es lo más útil técnicamente: si dice `PENDIENTE MIGRACION` en rojo, no sigas trabajando — avisá al admin.

## Pantallas relacionadas

- **`FrmRespaldos`** — gestión de copias de seguridad.
- **`FrmPerfil`** — datos del usuario actual.
- **`FrmLogAuditoria`** — visor del log (Admin).
- **`FrmUsuarios`** — administración de usuarios (Admin).
