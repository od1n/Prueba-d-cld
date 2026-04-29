# 00 · Introducción

## ¿Qué es Sistema de Gestión?

Sistema de Gestión es una aplicación de escritorio (Windows) que centraliza la operación administrativa y productiva de la empresa. Cubre:

- **Ventas**: facturación electrónica y de notas de entrega, cuentas por cobrar.
- **Compras y gastos**: egresos, cuentas por pagar, gestión de proveedores.
- **Inventario y producción**: catálogo, kardex, registro diario, moldes, mermas.
- **Nómina**: empleados, cálculo semanal, préstamos, prestaciones LOTTT.
- **Reportes**: rentabilidad, flujo de caja, libro IVA, comparativos.
- **Doble moneda**: todos los montos manejan **bolívares (Bs)** y **dólares (USD)** con tasa BCV y paralelo registrados por fecha.

## Requisitos para empezar

1. **Usuario y contraseña** (entregados por el administrador).
2. **PC con Windows** y la aplicación instalada (ícono *Sistema de Gestión* en el escritorio).
3. **Tasa BCV del día** cargada (el sistema la pide automáticamente en el primer login del día).

## Iniciar sesión

1. Doble clic en el ícono **Sistema de Gestión**.
2. En la pantalla de **Login**, ingresar usuario y contraseña.
3. Hacer clic en **Entrar**.

> ⚠ Si es la **primera vez** que entras, el sistema te pedirá cambiar la contraseña antes de continuar. La nueva contraseña debe tener al menos 8 caracteres.

> ⚠ Tras **5 intentos fallidos** la cuenta se bloquea temporalmente. Solicita desbloqueo a un administrador.

## Pantalla de bienvenida (alertas)

Si hay alertas activas, al iniciar sesión se abrirá una ventana con el resumen:

- **Stock bajo**: productos por debajo del mínimo configurado.
- **CxC vencidas**: facturas pendientes de cobro vencidas.
- **Mantenimientos vencidos**: moldes con mantenimiento programado pasado de fecha.

Cerrá la ventana con **OK** para continuar al menú principal. Las alertas también quedan visibles permanentemente en la franja superior del menú principal (clic en *ALERTAS* para volver a verlas).

## Carga de la tasa BCV del día

En el primer login de cada día, si la tasa BCV no está registrada, se abre automáticamente la pantalla **Actualizar tasa**. Tenés tres opciones:

- **Cargar la tasa**: ingresá los valores BCV y paralelo del día.
- **Buscar automática**: el sistema consulta el sitio del BCV y propone la tasa.
- **Posponer hoy**: pospone la carga hasta mañana (no recomendado, los reportes pueden quedar inexactos).

## El menú principal

El menú principal (`MenuPrincipalV2`) tiene cinco zonas:

```
┌──────────────────────────────────────────────────────────────────────────┐
│  ⚙ SistemaGestion                  [F3 Buscar][Perfil][Ayuda] usuario [X]│  ← navbar
├──────────────────────────────────────────────────────────────────────────┤
│  Buenos días, Admin   BCV: 36,50  PAR: 41,20    ALERTAS: 2 stock         │  ← franja info
│  miércoles, 28 de abril de 2026     ● actualizada hoy        [↻ Tasa]    │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   [▣ Dashboard]    [◈ Transacciones]    [◧ Archivos]                     │  ← módulos
│                                                                          │
│   [⊞ Producción]   [◎ Nómina]                                            │
│                                                                          │
├──────────────────────────────────────────────────────────────────────────┤
│   [▤ Reportes]   [$ Tasa]   [⚙ Sistema]                       [✕ Salir]  │  ← barra inferior
└──────────────────────────────────────────────────────────────────────────┘
```

- **Navbar (azul oscuro arriba)**: nombre del sistema, atajo de búsqueda **F3**, perfil, ayuda contextual, datos del usuario y **Cerrar sesión**.
- **Franja de información (banda blanca)**: saludo, fecha, **tasa BCV/Paralelo**, sello de frescura de la tasa, alertas activas y botón **↻ Actualizar tasa**.
- **Centro (módulos)**: cinco tarjetas grandes — **Dashboard**, **Transacciones**, **Archivos**, **Producción**, **Nómina**. Las tarjetas de los módulos a los que no tenés permiso aparecen apagadas.
- **Barra inferior**: accesos a **Reportes**, **Tasa de cambio**, **Sistema** y **Salir**.

> **Atajo:** desde cualquier pantalla del sistema, presionar **F3** abre el buscador global.

## Cerrar sesión vs Salir

- **Cerrar sesión** (botón rojo arriba a la derecha): cierra tu sesión y vuelve al login. Otra persona puede entrar sin reabrir el programa. *Se hace un respaldo automático antes de cerrar.*
- **Salir** (botón rojo abajo a la derecha): cierra el programa por completo.

## Convención de doble moneda

Todos los montos se almacenan en **dos monedas**: bolívares (Bs) y su equivalente en dólares (USD), calculado con la tasa BCV del día. En las pantallas vas a ver:

- Campos donde podés ingresar **Bs o USD** (un selector de moneda al lado).
- Totales que muestran ambos montos: `Total: Bs 1.500,00  ($ 41,10)`.
- En reportes podés elegir mostrar Bs, USD o ambos.

> ⚠ Si la tasa del día no está cargada, todos los cálculos en USD muestran `—` y los reportes salen incompletos. **Cargá la tasa siempre al inicio del día.**

## ¿Dónde busco ayuda en cada pantalla?

Cada pantalla tiene un botón **? Ayuda** o el atajo **F1**. Al presionarlo se abre la sección correspondiente de este manual.

## Próximos capítulos

Continuá con el módulo que vas a usar. Si no sabés por dónde empezar, **Transacciones → Facturas** es el flujo más común para una jornada de ventas, y **Producción → Registro diario** para una jornada de planta.
