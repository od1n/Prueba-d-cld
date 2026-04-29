# `FrmLogin` — Inicio de sesión

> Pantalla de entrada al sistema. Valida usuario y contraseña, fuerza el cambio de contraseña inicial y bloquea la cuenta tras varios intentos fallidos.

## Cómo llegar

- **Ruta**: aparece automáticamente al abrir el programa (doble clic en el ícono *Sistema de Gestión*).
- **Atajo**: ninguno.
- **Permiso requerido**: ninguno (todos los usuarios deben pasar por aquí).

## Para qué se usa

Es el primer paso obligatorio al abrir el sistema. Pide tu **usuario** y **contraseña** para identificarte y cargar tus permisos. Si tu cuenta es nueva, o el administrador la marcó para cambio obligatorio, el sistema te llevará directo al diálogo de cambio de contraseña antes de dejarte entrar.

## Antes de empezar

- Tener un **usuario y contraseña** entregados por el administrador.
- Si es la **primera vez** que un Administrador entra, las credenciales por defecto son `admin` / `admin123`. El sistema te obligará a cambiarlas inmediatamente.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────┐
│        SISTEMA DE GESTIÓN                │  ← banner azul
├──────────────────────────────────────────┤
│        Ingresa tus credenciales          │
│                                          │
│   Usuario                                │
│   [___________________________]          │
│                                          │
│   Contraseña                             │
│   [● ● ● ● ● ● ● ● _________]            │
│                                          │
│   [ ] Mostrar contraseña                 │
│                                          │
│   [          Entrar           ]          │  ← botón principal
│                                          │
├──────────────────────────────────────────┤
│  (mensaje de error si lo hay)            │  ← franja roja
└──────────────────────────────────────────┘
```

| Campo / botón | Para qué sirve |
|---|---|
| **Usuario** | Tu nombre de inicio de sesión (sin espacios, en minúsculas). |
| **Contraseña** | Tu clave personal. Se muestra con puntos por defecto. |
| **Mostrar contraseña** | Casilla para ver lo que escribís en texto plano (útil si te equivocás). |
| **Entrar** | Verifica las credenciales y abre el menú principal. También se activa con `Enter`. |

> **Tip:** podés saltar entre los campos con `Tab` o `Enter`. Estando en *Contraseña*, `Enter` envía el formulario directamente.

## Casos de uso

### Caso 1 — Inicio de sesión normal

**Pasos**:

1. Escribir el **Usuario**.
2. Presionar `Enter` o `Tab` para pasar al campo de contraseña.
3. Escribir la **Contraseña**.
4. Presionar `Enter` o hacer clic en **Entrar**.

**Resultado**: el botón cambia a "Verificando…" mientras se valida. Si las credenciales son correctas, se cierra el login y aparecen las **alertas pendientes** (si hay) seguidas del menú principal.

### Caso 2 — Primer login del Administrador

**Cuándo**: la primera vez que se abre el sistema, o cuando el admin todavía tiene la contraseña inicial `admin123`.

**Pasos**:

1. Ingresar `admin` / `admin123`.
2. Aparece el aviso `Primer acceso detectado. Debe cambiar la contraseña antes de continuar. La contraseña inicial 'admin123' no es segura.`
3. Se abre el diálogo **Cambio de contraseña obligatorio** (no se puede cerrar con la X).
4. Establecer una contraseña nueva de **al menos 8 caracteres**.
5. **Guardar contraseña**.

**Resultado**: la nueva contraseña queda activa y el sistema continúa al menú principal.

### Caso 3 — Cambio de contraseña forzado por el administrador

**Cuándo**: el admin reseteó tu contraseña o marcó tu cuenta para cambio obligatorio.

**Pasos**:

1. Ingresar usuario y la contraseña temporal.
2. Aparece el aviso `Su contraseña debe ser cambiada antes de continuar.`
3. En el diálogo, escribir la contraseña actual y la nueva (mínimo 8 caracteres).
4. **Guardar contraseña**.

**Resultado**: si cancelás el cambio, el sistema te bota con el mensaje `Debe cambiar la contraseña para acceder al sistema.`

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingresa tu nombre de usuario.` | El campo Usuario está vacío | Llenar el campo |
| `Ingresa tu contraseña.` | El campo Contraseña está vacío | Llenar el campo |
| `Usuario o contraseña incorrectos.` | Credenciales inválidas | Verificar mayúsculas, espacios, bloq mayús |
| `Demasiados intentos fallidos. La cuenta ha sido bloqueada 30 minutos.` | 5 intentos fallidos seguidos en esta sesión | El programa se cierra solo a los 2,5 segundos. Esperar 30 minutos o pedir desbloqueo al administrador |
| `Debe cambiar la contraseña para acceder al sistema.` | Cancelaste el cambio obligatorio | Volver a iniciar sesión y completar el cambio |
| `Cambio de contraseña requerido` | Tu cuenta requiere cambio antes de continuar | Hacer clic en OK y seguir el flujo del Caso 2 o 3 |

## Consejos

- **No compartas tu contraseña.** Cada usuario tiene un rol y todas las acciones quedan en el log de auditoría asociadas a tu nombre.
- Marcá **Mostrar contraseña** sólo si estás seguro de que nadie ve la pantalla.
- Después de **5 intentos fallidos** el programa se cierra y registra los intentos en auditoría — pedile al administrador que revise el log si pensás que alguien intentó usar tu cuenta.
- El botón **Entrar** se desactiva mientras valida; no hace falta apretarlo varias veces.

## Pantallas relacionadas

- **`FrmCambioContrasena`** — diálogo que aparece para cambio voluntario u obligatorio.
- **`FrmAlertas`** — ventana de alertas que aparece justo después del login si hay items pendientes.
- **`FrmActualizarTasa`** — primera pantalla del día si la tasa BCV no está cargada.
- **`MenuPrincipalV2`** — menú principal al que se llega tras un login exitoso.
