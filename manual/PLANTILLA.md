# Plantilla — Ficha de pantalla

Cada pantalla del sistema se documenta con esta estructura. Copia este archivo y rellena las secciones cuando documentes una nueva pantalla.

---

# `NombreDelFormulario` — Título amigable

> **Una línea** que describe para qué sirve la pantalla, leíble de un vistazo.

## Cómo llegar

- **Ruta**: *Menú principal → Módulo → Botón / submenú*
- **Atajo**: `F3` o ninguno
- **Permiso requerido**: `Admin` / `Ventas` / `Almacén` / `Contabilidad` / *(libre)*

## Para qué se usa

Párrafo breve (3–5 líneas) que explica el propósito operativo: qué problema resuelve, en qué momento del día/proceso se usa y quién lo usa típicamente (cajero, almacenista, contador…).

## Antes de empezar

Lista de requisitos previos, si aplica:

- Tener cargada la **tasa BCV del día** (si la pantalla usa montos en USD).
- Tener registrado al menos un **cliente activo** (si la pantalla genera facturas).
- Tener stock disponible (si descuenta inventario).

## Qué ves en la pantalla

Descripción de las áreas principales de la pantalla, en el orden en que el ojo las recorre:

- **Encabezado / barra superior**: qué muestra (totales, fechas, botones de acción).
- **Tabla central / panel principal**: qué información lista, columnas visibles.
- **Pie / barra inferior**: botones de acción (**Guardar**, **Imprimir**, **Cerrar**).

## Casos de uso

### Caso 1 — *Nombre claro de la operación*

**Cuándo**: descripción del momento o disparador (p. ej. *"cuando un cliente paga al contado"*).

**Pasos**:

1. Hacer clic en **Botón X**.
2. Llenar el campo **Nombre** con el dato Y.
3. Seleccionar **Z** del desplegable.
4. Hacer clic en **Guardar**.

**Resultado**: qué cambia tras guardar (mensaje de éxito, registro creado, impresión, etc.).

### Caso 2 — *Otra operación común*

(Repetir el formato del Caso 1.)

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| "El campo X es obligatorio" | Faltó llenar un dato | Llenar el campo y reintentar |
| "Stock insuficiente" | El producto no tiene unidades disponibles | Comprar / fabricar / reducir cantidad |
| "Tasa del día no cargada" | Falta la tasa BCV de hoy | Ir a *Tasa de cambio → Actualizar* |

## Consejos

- Tip operativo no obvio.
- Atajo o flujo rápido.
- Cosa que **no** se debe hacer.

## Pantallas relacionadas

- **`OtroFormulario`** — para qué se complementan.
- **`Reporte X`** — dónde ver el resultado consolidado.
