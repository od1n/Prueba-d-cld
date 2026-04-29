# `FrmBuscador` — Buscador global

> Ventana de búsqueda rápida en todo el sistema (clientes, proveedores, inventario, facturas, compras, producción y moldes). Se invoca con la tecla `F3` desde cualquier pantalla del menú principal.

## Cómo llegar

- **Ruta**: tecla **`F3`** desde el menú principal (también desde el botón **F3 Buscar** en la navbar).
- **Atajo**: `F3`.
- **Permiso requerido**: ninguno.

## Para qué se usa

Es el atajo más rápido para encontrar **cualquier registro** del sistema sin tener que navegar por los menús. Escribís parte del nombre, código, número o descripción y el buscador devuelve resultados agrupados por módulo. Al hacer clic en un resultado, abre el módulo correspondiente.

## Antes de empezar

- Tener al menos un registro cargado en alguno de los módulos buscables.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────────┐
│ Buscar en todo el sistema                                      │  ← header azul
│ [ Escribe aqui para buscar clientes, facturas, productos...  ] │
├────────────────────────────────────────────────────────────────┤
│   Escribe al menos 2 caracteres para buscar...                 │  ← estado
├────────────────────────────────────────────────────────────────┤
│   CLIENTES                                                     │  ← grupo
│   ┌──────────────────────────────────────────────────────┐     │
│   │ [Clientes]  Juan Pérez                            ›  │     │  ← tarjeta
│   │             RIF V-12345678 · Tel: 0414-1234567       │     │
│   └──────────────────────────────────────────────────────┘     │
│                                                                │
│   FACTURAS                                                     │
│   ┌──────────────────────────────────────────────────────┐     │
│   │ [Facturas]  Factura 00125                         ›  │     │
│   │             28/04/2026 · Total $ 1.200,00            │     │
│   └──────────────────────────────────────────────────────┘     │
├────────────────────────────────────────────────────────────────┤
│ ESC para cerrar  |  Enter para abrir el módulo seleccionado    │  ← footer
└────────────────────────────────────────────────────────────────┘
```

### Categorías que busca

| Categoría | Qué encuentra | Color del borde |
|---|---|---|
| **Clientes** | Por nombre, RIF, teléfono | Azul |
| **Proveedores** | Por nombre, RIF, teléfono | Naranja |
| **Inventario** | Por código o descripción | Verde |
| **Facturas** | Por número de factura, cliente | Azul claro |
| **Compras** | Por número o proveedor | Morado |
| **Ordenes de Produccion** | Por número o producto | Verde oliva |
| **Moldes** | Por código o descripción | Gris |
| **Kardex** | Movimientos de inventario | Gris |

## Casos de uso

### Caso 1 — Encontrar y abrir un cliente

**Pasos**:

1. Presionar **F3** (desde el menú principal).
2. Escribir parte del nombre, ej. `Pérez`.
3. Esperar 0,3 segundos (el sistema busca solo).
4. Hacer clic en la tarjeta del cliente.

**Resultado**: se abre la pantalla **Clientes** con el cliente seleccionado.

### Caso 2 — Buscar una factura por número

**Pasos**:

1. **F3**.
2. Escribir el número, ej. `125`.
3. Hacer clic en la tarjeta de la factura `Factura 00125`.

**Resultado**: se abre la pantalla **Facturas**.

### Caso 3 — Navegación con teclado

**Pasos**:

1. **F3**, escribir el término.
2. Presionar `↓` para mover el foco al primer resultado.
3. Usar `↑` / `↓` para moverte entre tarjetas, `Enter` para abrir, `↑` desde la primera tarjeta vuelve al campo de búsqueda.
4. `Esc` cierra la ventana.

**Resultado**: misma acción que con clic, pero más rápido para usuarios avanzados.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Escribe al menos 2 caracteres para buscar...` | El término es muy corto | Escribir más letras |
| `Buscando...` | El sistema está consultando | Esperar 0,3 a 1 segundo |
| `Sin resultados para "xxx"` | No hay coincidencias | Verificar la ortografía o probar un término más corto |

## Consejos

- **Búsqueda por partes** — el buscador encuentra coincidencias en cualquier parte del campo, así que `pere` encuentra `Pérez` y también `Pereira`.
- **El recuadro de la categoría** te dice de qué módulo viene el resultado antes de abrirlo.
- Cada búsqueda **queda registrada en el log de auditoría** con el término usado y el resultado abierto.
- Si trabajás mucho con un mismo módulo (p. ej. Facturas), conviene aprender el atajo `F3` y escribir 3-4 letras: es 5 veces más rápido que ir por el menú.
- `Esc` cierra inmediatamente — útil si abriste el buscador por error.

## Pantallas relacionadas

- **`MenuPrincipalV2`** — desde donde se invoca con F3.
- **`FrmClientes`**, **`FrmProveedores`**, **`FrmInventario`**, **`FrmFacturas`**, **`FrmCompras`**, **`FrmOrdenesProduccion`**, **`FrmMoldes`**, **`FrmKardex`** — destinos posibles al hacer clic en un resultado.
