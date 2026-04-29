# `FrmPerfil` — Mi perfil

> Pantalla personal del usuario actual. Tiene tres pestañas: información personal, cambio de contraseña con indicador de fuerza, y preferencias de la aplicación (color, KPIs, sonidos, filas por página).

## Cómo llegar

- **Ruta**: *Menú principal → barra superior → Perfil*. También desde *Sistema → Mi Perfil y preferencias*.
- **Atajo**: ninguno.
- **Permiso requerido**: ninguno (cada usuario edita el suyo).

## Para qué se usa

- Actualizar tu **información de contacto** (nombre completo, email, teléfono).
- **Cambiar la contraseña** sin pasar por el flujo forzado.
- **Personalizar la apariencia y comportamiento** del sistema para tu sesión: color de acento, mostrar KPIs, sonidos de alerta, confirmaciones de borrado, abrir el último módulo al iniciar, filas por página en las grillas.

## Antes de empezar

- Conocer tu **contraseña actual** si vas a cambiarla.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────┐
│ ┌──┐ Juan Pérez                                      │  ← banner con avatar
│ │JP│ @juan  |  Admin                                 │
│ └──┘                                                 │
├──────────────────────────────────────────────────────┤
│ [ Información personal ][ Cambiar contraseña ][ Pref.]│
├──────────────────────────────────────────────────────┤
│   Usuario:           juan   (sólo lectura)            │
│   Rol:               Admin  (sólo lectura)            │
│   Nombre completo:   [ Juan Pérez               ]     │
│   Email:             [ juan@empresa.com         ]     │
│   Teléfono:          [ 0414-1234567             ]     │
│   Miembro desde:     12/05/2024                       │
│                                                       │
│   [ Guardar información ]                             │
├──────────────────────────────────────────────────────┤
│                                            [ Cerrar ] │
└──────────────────────────────────────────────────────┘
```

### Banner

| Elemento | Para qué sirve |
|---|---|
| **Avatar circular** | Iniciales (hasta 2 letras) sobre fondo azul. |
| **Nombre completo** | Tal como se guardó. |
| **@usuario \| Rol** | Login y rol asignado. |

### Pestaña 1 — Información personal

| Campo | Editable | Comentario |
|---|---|---|
| **Usuario** | No | Tu login. Sólo el admin puede cambiarlo en `FrmUsuarios`. |
| **Rol** | No | Asignado por el admin. |
| **Nombre completo** | Sí | Aparece en encabezados, reportes, etc. |
| **Email** | Sí | Informativo. |
| **Teléfono** | Sí | Informativo. |
| **Miembro desde** | No | Fecha de creación de la cuenta. |
| **Guardar información** | — | Guarda los cambios y actualiza el saludo del menú principal. |

### Pestaña 2 — Cambiar contraseña

| Campo | Para qué sirve |
|---|---|
| **Contraseña actual** | Tu clave de hoy. |
| **Nueva contraseña** | La nueva (mínimo **6 caracteres** desde esta pestaña). |
| **Confirmar nueva** | Repetí la misma. El fondo se vuelve verde si coinciden, rojo si no. |
| **Indicador de fuerza** | Barra horizontal: rojo (Débil) → naranja (Regular) → amarillo (Buena) → verde (Fuerte). |
| **Cambiar contraseña** | Aplica el cambio. |

> El **diálogo modal `FrmCambioContrasena`** (forzado / primer login) exige 8 caracteres. Esta pestaña permite 6 mínimo. La buena práctica es usar 8+.

### Pestaña 3 — Preferencias

#### Apariencia

| Opción | Valores |
|---|---|
| **Color de acento** | Azul (default) · Verde · Morado · Naranja · Rojo · Gris |

#### Comportamiento

| Opción | Default | Qué hace |
|---|---|---|
| **Mostrar KPIs en el dashboard** | ✓ | Muestra/oculta los KPIs en el dashboard. |
| **Sonido en alertas de stock bajo** | — | Reproduce un sonido cuando aparece stock bajo. |
| **Confirmar antes de eliminar registros** | ✓ | Pide confirmación antes de eliminar (recomendado). |
| **Abrir último módulo al iniciar** | — | Reabre el último módulo donde trabajaste al iniciar sesión. |

#### Grillas

| Opción | Default | Rango |
|---|---|---|
| **Filas por página** | 50 | 10 a 500 |

### Pie

| Botón | Qué hace |
|---|---|
| **Cerrar** | Cierra la ventana. |

> Las preferencias se guardan al pulsar **Guardar preferencias** dentro de la pestaña. Algunos cambios (color de acento) se aplican al **reiniciar el programa**.

## Casos de uso

### Caso 1 — Actualizar mi nombre y teléfono

**Pasos**:

1. Pestaña **Información personal**.
2. Cambiar **Nombre completo** y/o **Teléfono**.
3. **Guardar información**.

**Resultado**: aparece `Informacion guardada correctamente.` y el menú principal muestra el nuevo nombre.

### Caso 2 — Cambiar mi contraseña

**Pasos**:

1. Pestaña **Cambiar contraseña**.
2. Llenar los tres campos.
3. Mirar el indicador de fuerza (apuntar a `Buena` o `Fuerte`).
4. **Cambiar contraseña**.

**Resultado**: aparece `Contrasena cambiada exitosamente. La nueva contraseña estara activa en el proximo inicio de sesion.`

### Caso 3 — Cambiar el color de acento

**Pasos**:

1. Pestaña **Preferencias**.
2. **Color de acento**: elegir del desplegable.
3. **Guardar preferencias**.
4. Cerrar el sistema y volver a abrir.

**Resultado**: el sistema usa el nuevo color en botones primarios y elementos destacados.

### Caso 4 — Aumentar filas por página

**Cuándo**: tu monitor es grande y querés ver más registros sin scroll.

**Pasos**:

1. Pestaña **Preferencias** → **Filas por página**: subir a 100 o 200.
2. **Guardar preferencias**.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingresa tu contrasena actual.` | Dejaste el campo vacío | Llenarlo |
| `La nueva contrasena y la confirmacion no coinciden.` | Tipeaste distinto en los dos campos | Volver a tipear |
| Mensaje de error de la BD al cambiar contraseña | Contraseña actual incorrecta o falla en BD | Verificar la contraseña actual |
| `Informacion guardada correctamente.` | Pestaña Info: éxito | — |
| `Contrasena cambiada exitosamente.` | Pestaña Contraseña: éxito | — |
| `Preferencias guardadas. Algunos cambios se aplican al reiniciar.` | Pestaña Preferencias: éxito | Reiniciar para ver el nuevo color |

## Consejos

- **Cambiá la contraseña periódicamente** — mínimo cada 90 días.
- El indicador de fuerza apunta a **`Fuerte`** con: 10+ caracteres + dígitos + símbolos.
- Si activás **"Sonido en alertas de stock bajo"** y trabajás con audífonos, escucharás un beep al ver el panel de alertas.
- **"Confirmar antes de eliminar"** evita errores costosos. No la desactives a menos que tengas mucha experiencia.

## Pantallas relacionadas

- **`FrmCambioContrasena`** — diálogo modal (forzado / primer login).
- **`FrmUsuarios`** — sólo el admin puede cambiar tu **rol** o tu nombre de usuario.
- **`MenuPrincipalV2`** — refleja tu nombre y color de acento.
