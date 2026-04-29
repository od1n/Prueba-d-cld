# `FrmCambioContrasena` — Cambio de contraseña

> Diálogo para cambiar tu contraseña. Aparece de dos formas: **forzado** (el sistema te obliga al primer login o cuando el admin lo solicita) y **voluntario** (desde *Mi Perfil*).

## Cómo llegar

- **Ruta forzada**: aparece automáticamente tras el login si tu cuenta lo requiere.
- **Ruta voluntaria**: *Sistema → Mi Perfil → pestaña "Cambiar contrasena"* (variante extendida con indicador de fuerza). Este diálogo independiente también se invoca desde otros flujos administrativos.
- **Atajo**: ninguno.
- **Permiso requerido**: ninguno (cada usuario cambia su propia contraseña).

## Para qué se usa

Permite establecer una contraseña nueva. En modo **forzado** no podés cerrar la ventana con la X — tenés que terminar el cambio o cancelar el inicio de sesión. En modo **voluntario** podés cancelar libremente.

## Antes de empezar

- Saber tu **contraseña actual** (excepto si es el primer login del Admin con `admin123`).
- Tener pensada una contraseña **de al menos 8 caracteres**. Idealmente con letras mayúsculas, minúsculas, números y un símbolo.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────┐
│ ⚠️  Establezca una nueva contraseña        │  ← banner azul (forzado)
├────────────────────────────────────────────┤
│  Contraseña actual:                        │
│  [● ● ● ● ● ● ● ● ____________]            │
│                                            │
│  Nueva contraseña:                         │
│  [● ● ● ● ● ● ● ● ____________]            │
│                                            │
│  Confirmar contraseña:                     │
│  [● ● ● ● ● ● ● ● ____________]            │
│                                            │
│  Mínimo 8 caracteres. Use letras, números  │  ← indicador de fortaleza
│  y símbolos.                               │
│                                            │
│  [ Guardar contraseña ]   [ Cancelar ]     │
└────────────────────────────────────────────┘
```

| Campo / botón | Para qué sirve |
|---|---|
| **Contraseña actual** | Tu clave actual (la que estás por reemplazar). |
| **Nueva contraseña** | La nueva clave. Mínimo 8 caracteres. |
| **Confirmar contraseña** | Repetí la misma para evitar errores de tipeo. |
| **Indicador de fortaleza** | Texto que cambia de color: rojo (débil) → naranja (aceptable) → verde (buena/fuerte). |
| **Guardar contraseña** | Aplica el cambio. |
| **Cancelar** | Cierra sin cambiar (sólo en modo voluntario; en forzado no funciona). |

### Indicador de fortaleza (qué considera el sistema)

| Mensaje | Cuándo aparece |
|---|---|
| `Contraseña débil — añada letras mayúsculas, números y símbolos.` | Sólo cumple con el mínimo de longitud |
| `Contraseña aceptable — podría ser más segura.` | Cumple 2 de los criterios (longitud, mayúsculas, números, símbolos) |
| `Contraseña buena.` | Cumple 3 criterios |
| `✓ Contraseña fuerte.` | Cumple 4 o 5 criterios |

## Casos de uso

### Caso 1 — Cambio voluntario

**Pasos**:

1. Llenar **Contraseña actual** con la que estás usando ahora.
2. Llenar **Nueva contraseña** y mirar el indicador de fortaleza.
3. Repetir en **Confirmar contraseña**.
4. **Guardar contraseña**.

**Resultado**: aparece el aviso `Contraseña actualizada correctamente.` y el diálogo se cierra.

### Caso 2 — Cambio forzado por primer login

**Pasos**: igual que el Caso 1, pero **no se puede cancelar**. Si intentás cerrar con la X aparece `Debe establecer una nueva contraseña para continuar.`

**Resultado**: tras guardar, volvés al login (en algunos flujos) o continuás directo al menú principal.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Las contraseñas no coinciden.` | "Nueva" y "Confirmar" son distintas | Volver a escribir las dos iguales |
| `La contraseña debe tener al menos 8 caracteres.` | La nueva contraseña es muy corta | Usar 8 o más caracteres |
| `La contraseña actual es incorrecta.` (u otro error de validación) | La contraseña actual no coincide con la registrada | Verificar mayúsculas / bloq mayús; pedir reseteo al admin si la olvidaste |
| `Debe establecer una nueva contraseña para continuar.` | Intentaste cerrar el diálogo en modo forzado | Completar el cambio |

## Consejos

- Usá una contraseña distinta para cada sistema. **Nunca** la misma del email o el banco.
- Combiná letras, números y un símbolo (`@ # $ % & *`).
- Si te la olvidás, no hay forma de recuperarla — el administrador tiene que **resetearla**, lo que te marca para cambio forzado en el próximo login.

## Pantallas relacionadas

- **`FrmLogin`** — desde donde dispara el cambio forzado.
- **`FrmPerfil`** — pestaña "Cambiar contrasena" para cambios voluntarios con indicador de barra.
- **`FrmUsuarios`** — el administrador la usa para resetear contraseñas de otros usuarios.
