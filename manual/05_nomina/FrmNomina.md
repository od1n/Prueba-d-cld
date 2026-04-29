# `FrmNomina` — Submenú de Nómina

> Pantalla **lanzadora** del módulo de Nómina. Cuatro tarjetas que abren las pantallas operativas: empleados, procesar nómina, préstamos y beneficios LOTTT.

## Cómo llegar

- **Ruta**: *Menú principal → Nómina*.
- **Permiso requerido**: `Nomina` (o `Admin`).

## Para qué se usa

Es solo el **menú** del módulo. No realiza operaciones por sí mismo: cada tarjeta abre la pantalla correspondiente. Pensado para que el responsable de nómina tenga todo a un clic.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────┐
│ Módulo de Nómina                                          │
├──────────────────────────────────────────────────────────┤
│                                                           │
│  ┌──────────────────────┐  ┌─────────────────────────┐   │
│  │ ◎ Empleados          │  │ ▣ Procesar Nómina       │   │
│  │   Catálogo y altas   │  │   Nómina semanal        │   │
│  └──────────────────────┘  └─────────────────────────┘   │
│  ┌──────────────────────┐  ┌─────────────────────────┐   │
│  │ ◫ Préstamos          │  │ ◈ Beneficios LOTTT      │   │
│  │   Préstamos internos │  │   Utilidades y prestac. │   │
│  └──────────────────────┘  └─────────────────────────┘   │
├──────────────────────────────────────────────────────────┤
│                       [ Cerrar ]                          │
└──────────────────────────────────────────────────────────┘
```

### Tarjetas

| Tarjeta | Abre | Para qué |
|---|---|---|
| **◎ Empleados** | `FrmEmpleados` | Alta, edición y baja de empleados (datos personales, sueldo, tarifas). |
| **▣ Procesar Nómina** | `FrmProcesarNomina` | Crear y cerrar el período semanal, calcular sueldos y aportes patronales. |
| **◫ Préstamos** | `FrmPrestamos` | Otorgar préstamos a empleados; las cuotas se descuentan de la nómina. |
| **◈ Beneficios LOTTT** | `FrmBeneficiosLOTTT` | Calcular utilidades, vacaciones, bono vacacional y prestaciones sociales. |

Las tarjetas se iluminan al pasar el mouse y se abren con un clic.

## Consejos

- El flujo típico de la semana es: 1) actualizar **Empleados** si hay altas/bajas, 2) abrir **Procesar Nómina**, crear el período de la semana y calcular sueldos, 3) generar planillas (recibo, IVSS, aportes patronales).
- Los **préstamos activos** se descuentan automáticamente al calcular la nómina semanal.
- Los **beneficios LOTTT** se calculan una vez al año (o cuando corresponda por aniversario de ingreso).

## Pantallas relacionadas

- **`FrmEmpleados`** — catálogo de empleados.
- **`FrmProcesarNomina`** — proceso semanal.
- **`FrmPrestamos`** — préstamos internos.
- **`FrmBeneficiosLOTTT`** — utilidades y prestaciones.
