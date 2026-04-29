# `FrmUsuarios` — Gestión de usuarios

> Pantalla del administrador para crear, editar, activar/desactivar usuarios y asignarles roles. Soporta los cuatro roles base del sistema: Admin, Ventas, Almacén, Contabilidad.

## Cómo llegar

- **Ruta**: *Menú principal → ⚙ Sistema → Gestión de Usuarios*.
- **Atajo**: ninguno.
- **Permiso requerido**: 🔒 **Admin**.

## Para qué se usa

- **Dar de alta** nuevos usuarios cuando se contrata personal o cambia el equipo.
- **Editar** datos (nombre completo, contraseña, roles).
- **Activar / desactivar** cuentas — un usuario desactivado no puede iniciar sesión pero su historial queda.
- **Asignar roles** que controlan qué puede hacer cada uno (ver el anexo *Roles y permisos*).

## Antes de empezar

- Tener rol Admin.
- Conocer qué responsabilidad tiene la persona (ventas, almacén, contabilidad) — eso define el rol.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Gestión de Usuarios del Sistema                                          │  ← header
├──────────────────────────────────────────────────────────────────────────┤
│ [+ Nuevo] [✏ Editar] [⏸ Activar/Desactivar]                              │  ← barra
├──────────────────────────────────────────────────────────────────────────┤
│ Id  NombreCompleto    Usuario   Roles          Estado    UltimoAcceso    │
│ 1   Administrador     admin     Admin          Activo    28/04 08:14     │
│ 2   Juan Pérez        juan      Ventas         Activo    28/04 09:00     │
│ 3   María García      maria     Almacen        Activo    27/04 17:30     │
│ 4   Pedro Días        pedro     Contabilidad   Inactivo  Hace 30 días    │
├──────────────────────────────────────────────────────────────────────────┤
│ Usuarios: 4   |   Activos: 3   |   Inactivos: 1                          │
│                                                              [ Cerrar ]  │
└──────────────────────────────────────────────────────────────────────────┘
```

### Botones

| Botón | Para qué sirve |
|---|---|
| **+ Nuevo** | Abre el diálogo para crear un usuario. |
| **✏ Editar** | Abre el diálogo con los datos del usuario seleccionado. |
| **⏸ Activar/Desactivar** | Alterna el estado del usuario seleccionado (no aplica a Admin). |
| **Cerrar** | Cierra la ventana. |

> Doble clic en una fila también abre el diálogo de edición.

### Tabla

| Columna | Significado |
|---|---|
| **NombreCompleto** | Nombre real (aparece en saludos y reportes). |
| **NombreUsuario** | Login (sin espacios, en minúsculas). |
| **Roles** | Rol asignado (Admin / Ventas / Almacen / Contabilidad). |
| **Estado** | `Activo` o `Inactivo`. Inactivos en gris cursiva. |
| **UltimoAcceso** | Última vez que entró (`Nunca` si nunca lo hizo). |
| **FechaCreacion** | Cuándo se creó la cuenta. |

> Clic en el encabezado de **NombreCompleto**, **NombreUsuario** o **Roles** ordena por esa columna.

## Diálogo de Nuevo / Editar usuario

```
┌─────────────────────────────────────────────────┐
│ 👤  Nuevo Usuario                               │
├─────────────────────────────────────────────────┤
│ Nombre completo:    [ Juan Pérez            ]   │
│ Usuario (login):    [ juan                  ]   │
│ Contraseña:         [ ●●●●●●●●              ]   │
│ Confirmar:          [ ●●●●●●●●              ]   │
│                                                 │
│ Roles asignados:                                │
│ ┌─────────────────────────────────────────┐     │
│ │ [ ] Admin (acceso total — exclusivo)    │     │
│ │ [✓] Ventas (Facturas, Clientes, CxC)    │     │
│ │ [ ] Almacén (Inventario, Kardex, Moldes)│     │
│ │ [ ] Contabilidad (Reportes, Gastos, IVA)│     │
│ └─────────────────────────────────────────┘     │
│                                                 │
│ [✓] Usuario activo                              │
│                                                 │
│   [ Crear Usuario ]    [ Cancelar ]             │
└─────────────────────────────────────────────────┘
```

### Reglas del diálogo

- **Usuario (login)**: en alta es obligatorio; en edición está bloqueado (no se puede cambiar).
- **Contraseña**: obligatoria al crear; opcional al editar (si dejás vacío no se cambia).
- **Mínimo 6 caracteres** desde este diálogo.
- **Roles**:
  - Al menos uno marcado.
  - **Admin es exclusivo**: si lo marcás, los demás se desmarcan y deshabilitan; si marcás cualquier otro, Admin se desmarca solo.
  - El usuario `Admin` (cuenta principal) no se puede desactivar desde aquí.
- **Usuario activo**: si lo desmarcás, no podrá iniciar sesión.

## Casos de uso

### Caso 1 — Crear un nuevo vendedor

**Pasos**:

1. **+ Nuevo**.
2. **Nombre completo**: `Ana Rodríguez`.
3. **Usuario (login)**: `ana`.
4. **Contraseña / Confirmar**: una temporal de 8+ caracteres.
5. **Roles**: marcar **Ventas**.
6. Dejar **Usuario activo** marcado.
7. **Crear Usuario**.

**Resultado**: aparece `Usuario creado correctamente.` y la fila se agrega a la tabla. La primera vez que Ana entre, podrá usar la contraseña temporal — recomendable marcar la cuenta para *cambio forzado* (esto se hace por separado o entregándole la contraseña con instrucciones de cambiarla).

### Caso 2 — Cambiar el rol de un usuario

**Cuándo**: alguien promociona o cambia de área.

**Pasos**:

1. Seleccionar el usuario.
2. **✏ Editar** (o doble clic).
3. Cambiar las casillas de roles según corresponda.
4. **Guardar Cambios**.

**Resultado**: aparece `Usuario actualizado correctamente.`

### Caso 3 — Resetear la contraseña de un usuario

**Cuándo**: el usuario olvidó la suya.

**Pasos**:

1. Editar el usuario.
2. **Contraseña**: escribir una temporal (mínimo 6 caracteres).
3. **Confirmar**: la misma.
4. **Guardar Cambios**.

**Resultado**: la contraseña queda actualizada. Comunicale la nueva al usuario y pedile que la cambie en su próximo login.

### Caso 4 — Desactivar a alguien que dejó la empresa

**Cuándo**: un empleado se va. **No se puede borrar** (rompería el historial), pero sí desactivar.

**Pasos**:

1. Seleccionar el usuario.
2. **⏸ Activar/Desactivar**.
3. Confirmar el aviso `¿Desea desactivar al usuario 'Juan Pérez'?`.

**Resultado**: el usuario queda en gris cursiva con estado `Inactivo`. No puede iniciar sesión más, pero sus facturas, registros y log siguen intactos.

### Caso 5 — Reactivar un usuario

**Pasos**: igual que el caso 4, pero el aviso dice `¿Desea activar...?`.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `El nombre completo es obligatorio.` | Falta el nombre | Llenar el campo |
| `El nombre de usuario es obligatorio.` | Falta el login | Llenarlo |
| `Debe seleccionar al menos un rol.` | Sin rol marcado | Marcar al menos uno |
| `Admin es exclusivo y no puede combinarse.` | Marcaste Admin junto con otro rol | Desmarcar los otros o desmarcar Admin |
| `La contraseña es obligatoria.` | Crear sin contraseña | Ingresarla |
| `La contraseña debe tener al menos 6 caracteres.` | Contraseña muy corta | Usar 6 o más |
| `Las contraseñas no coinciden.` | Confirmación distinta | Volver a escribir igual |
| `El usuario Admin no puede desactivarse.` | Intentaste desactivar al admin | El admin principal está protegido |
| `Usuario creado correctamente.` / `Usuario actualizado correctamente.` | Éxito | — |

## Consejos

- **Un usuario por persona**, no compartir cuentas. Sino la auditoría pierde valor.
- **No le des Admin a nadie que no lo necesite** — el admin puede ver y modificar todo, incluyendo el log de auditoría.
- Cuando alguien se va, **desactivá la cuenta el mismo día** y rotá las contraseñas de servicios externos compartidos.
- Para **nombres de usuario**, usar el formato `nombre` (corto, en minúsculas, sin espacios). Ej: `juan`, `maria.g`.
- **Las cuentas inactivas no consumen licencias ni espacio**, pero sí mantienen el historial — ideal para auditorías futuras.

## Pantallas relacionadas

- **`FrmSistema`** — desde donde se abre esta pantalla.
- **`FrmCambioContrasena`** — el usuario la usa para cambiar su contraseña tras un reseteo.
- **`FrmLogAuditoria`** — registra todas las acciones de creación, edición y toggle de estado.
- **Anexo `roles_permisos.md`** — detalle de qué puede hacer cada rol.
