# Atajos de teclado

> Lista de atajos disponibles en el sistema. La mayoría son globales (funcionan desde cualquier pantalla del menú principal); algunos son específicos del contexto.

## Atajos globales

Funcionan estando en el **menú principal** (algunos también dentro de pantallas hijas).

| Atajo | Acción | Dónde funciona |
|---|---|---|
| **`F3`** | Abrir el **Buscador global** (clientes, facturas, productos, etc.) | Menú principal |
| **`Esc`** | Cerrar el diálogo activo | Buscador, diálogos modales |
| **`Enter`** | Confirmar / aceptar el botón principal | Login, diálogos de creación |
| **`Tab`** | Saltar al siguiente campo | Todos los formularios |
| **`Shift + Tab`** | Saltar al campo anterior | Todos los formularios |

> ⚠ **No hay un atajo F1 / Ayuda contextual** unificado en esta versión. La ayuda se abre con el botón **? Ayuda** que aparece en cada módulo.

## Atajos del Login

| Atajo | Acción |
|---|---|
| **`Enter`** en *Usuario* | Pasa el foco a *Contraseña* |
| **`Enter`** en *Contraseña* | Pulsa el botón **Entrar** |

## Atajos del Buscador (F3)

| Atajo | Acción |
|---|---|
| **`F3`** | Abrir el buscador (desde el menú) |
| **`↓`** (estando en el campo de búsqueda) | Mover el foco a la primera tarjeta de resultados |
| **`↑`** / **`↓`** (entre tarjetas) | Navegar resultados |
| **`Enter`** (en una tarjeta) | Abrir el módulo del resultado |
| **`↑`** desde la primera tarjeta | Volver al campo de búsqueda |
| **`Esc`** | Cerrar el buscador |

## Atajos en grillas (DataGridView)

| Atajo | Acción |
|---|---|
| **`Doble clic`** en una fila | Abre el detalle / formulario de edición |
| **`Clic`** en encabezado de columna | Ordena por esa columna (un segundo clic invierte) |
| **`Ctrl + C`** | Copia la celda seleccionada al portapapeles |
| **`PgUp / PgDn`** | Avanza o retrocede una página visible |
| **`Home / End`** | Va al primer / último registro |

## Atajos en diálogos modales

| Atajo | Acción |
|---|---|
| **`Enter`** | Activa el botón primario (si lo hay configurado como AcceptButton) |
| **`Esc`** | Cancela / cierra el diálogo (si lo hay configurado como CancelButton) |
| **`Alt + letra subrayada`** | Activa el botón con esa letra subrayada (raro, pero posible) |

## Atajos específicos de Facturas

| Atajo | Acción |
|---|---|
| **`Enter`** en grilla de líneas | Pasa al siguiente campo editable |
| **`Tab`** en grilla de líneas | Avanza entre cantidad / precio |

## Atajos específicos del Log de auditoría

| Atajo | Acción |
|---|---|
| **`Enter`** en el campo *Buscar* | Aplica el filtro y refresca la tabla |

## Atajos del campo *Tasa BCV* / *Tasa Paralelo*

| Atajo | Acción |
|---|---|
| **`↑` / `↓`** | Aumenta o disminuye el valor (NumericUpDown) |
| Entrar al campo (foco) | Selecciona todo el contenido para reemplazar |

## Buenas prácticas

- **Aprendé `F3`**: es el atajo más útil del sistema. Buscar un cliente o factura desde F3 es 5x más rápido que ir por el menú.
- **`Enter` en formularios** dispara el botón primario (Guardar, Aceptar). Útil para no soltar el teclado.
- **`Esc`** cierra cualquier diálogo modal — siempre tenés salida sin tocar el mouse.
- En las grillas, **clic en el encabezado** ordena. Doble clic en una fila abre el detalle.
