# `FrmRespaldos` — Gestión de respaldos

> Lista los respaldos de base de datos disponibles, permite crear uno manual, verificar la integridad de los existentes y abrir la carpeta de respaldos en el Explorador de Windows.

## Cómo llegar

- **Ruta**: *Menú principal → ⚙ Sistema → Ver respaldos*. También accesible desde *Archivos → Respaldos* en algunas configuraciones.
- **Atajo**: ninguno.
- **Permiso requerido**: 🔒 **Admin**.

## Para qué se usa

Es la pantalla del administrador para garantizar que **siempre haya un respaldo reciente** de la base de datos. Permite:

- Crear un **respaldo manual** ahora mismo.
- Ver todos los respaldos (manuales y automáticos) con fecha, hora, tamaño y tipo.
- **Verificar la integridad** de los archivos (detecta corrupción o alteración).
- **Abrir la carpeta** en Windows para copiarlos a un disco externo o nube.

> El sistema crea respaldos automáticos al cerrar sesión. La retención por defecto es **30 días** (los más antiguos se eliminan).

## Antes de empezar

- Tener rol Admin.
- Espacio en disco para crear respaldos nuevos (cada uno suele ser pocos MB pero depende del volumen de datos).

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Respaldos de Base de Datos     [? Ayuda][+ Respaldar ahora]              │  ← header
│                                [Verificar][Abrir carpeta]                │
├──────────────────────────────────────────────────────────────────────────┤
│ Carpeta: C:\Users\...\Respaldos                                          │  ← ruta
├──────────────────────────────────────────────────────────────────────────┤
│ Archivo                       Fecha       Hora      Tamanio  Tipo  Dias  │
│ backup_2026-04-28_141023.db   28/04/2026  14:10:23  2,3 MB   Manual  Hoy │
│ backup_2026-04-28_080015.db   28/04/2026  08:00:15  2,3 MB   Auto    Hoy │
│ backup_2026-04-27_180000.db   27/04/2026  18:00:00  2,3 MB   Auto    1   │
│ ...                                                                      │
├──────────────────────────────────────────────────────────────────────────┤
│ Respaldos: 28 | Espacio: 64 MB | Retencion: 30 dias | Proximo: Mañana    │
│                                                              [ Cerrar ]  │
└──────────────────────────────────────────────────────────────────────────┘
```

### Botonera (cabecera)

| Botón | Qué hace |
|---|---|
| **? Ayuda** | Abre la ayuda contextual de este módulo. |
| **+ Respaldar ahora** | Crea un respaldo manual del estado actual de la base. |
| **Verificar** | Recorre todos los archivos y verifica su firma de integridad. |
| **Abrir carpeta** | Abre el Explorador de Windows en la carpeta de respaldos (útil para copiar al exterior). |

### Tabla de respaldos

| Columna | Significado |
|---|---|
| **Archivo** | Nombre del archivo de respaldo. |
| **Fecha** | Día en que se creó. |
| **Hora** | Hora exacta. |
| **Tamanio** | Tamaño en bytes formateado (KB / MB). |
| **Tipo** | `Manual` (creado a propósito) · `Automatico` (creado por el sistema al cerrar sesión, etc.). |
| **Dias** | `Hoy` · `Hace N dia(s)`. |

> **Color de fila**: las del día se muestran en verde claro. Las **manuales** aparecen con texto azul para distinguirlas.

### Pie (resumen)

- **Respaldos**: cantidad total.
- **Espacio**: total ocupado por todos los archivos.
- **Retención**: política de borrado (por defecto 30 días).
- **Próximo**: cuándo se hará el siguiente automático (`Mañana` o `Al reiniciar el sistema`).

## Casos de uso

### Caso 1 — Crear un respaldo manual antes de una operación sensible

**Cuándo**: vas a hacer un cambio masivo (importar productos, ajustar precios, anular varias facturas) y querés un punto de retorno.

**Pasos**:

1. **+ Respaldar ahora**.
2. Esperar 1-3 segundos.

**Resultado**: aparece el aviso

```
Respaldo creado exitosamente.

Archivo: backup_2026-04-28_141023.db
Tamanio: 2,3 MB
Hora:    28/04/2026 14:10:23
```

El archivo aparece como primera fila de la tabla con tipo `Manual`.

### Caso 2 — Verificar integridad

**Cuándo**: sospechás que alguien manipuló un archivo o la PC tuvo problemas.

**Pasos**:

1. **Verificar**.
2. Esperar a que termine de revisar todos los archivos.

**Resultado**: si todo está bien:

```
Todos los respaldos son integros (28 verificados).
```

Si hay problemas:

```
ADVERTENCIA: 1 respaldo(s) pueden estar danados o alterados.
27 respaldo(s) son correctos.
```

> En ese caso, **no usar el respaldo dañado**. Identificar cuál es por la fecha y eliminarlo desde el Explorador.

### Caso 3 — Copiar respaldos a un disco externo o nube

**Pasos**:

1. **Abrir carpeta**.
2. En el Explorador de Windows, seleccionar los archivos `backup_*.db` recientes.
3. Copiar al pendrive / OneDrive / Dropbox.

**Resultado**: tenés una copia fuera de la PC para casos de robo, falla de disco o ransomware.

> 🔒 **Recomendación de seguridad**: hacé esto al menos una vez por semana.

### Caso 4 — Restaurar un respaldo

> Esta pantalla **no restaura** respaldos directamente. La restauración la hace el equipo técnico (o el admin con guía técnica) reemplazando el archivo de base de datos por la copia deseada cuando el sistema está cerrado. Llamá al soporte antes de intentarlo.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Respaldo creado exitosamente.` | El respaldo se generó OK | Es informativo |
| `No se pudo crear el respaldo: …` | Falla al crear (permisos de carpeta, disco lleno, etc.) | Verificar espacio en disco; revisar que la carpeta exista; reintentar |
| `No hay respaldos para verificar.` | La carpeta está vacía | Crear al menos uno con **+ Respaldar ahora** |
| `Todos los respaldos son integros (N verificados).` | OK | — |
| `ADVERTENCIA: N respaldo(s) pueden estar danados o alterados.` | Hash inválido | No usar esos archivos. Eliminar y crear uno nuevo |

## Consejos

- **Hacé un respaldo manual antes de cada operación grande** (cambio masivo de precios, anulación múltiple, importación de datos).
- Los respaldos están **encriptados** (extensión `.db.enc`) — no los podés abrir directo con Excel o un editor de bases.
- **Llevate copias afuera**. Un respaldo que vive en la misma PC no protege contra robo o falla de disco.
- La **retención de 30 días** es generosa, pero recordá: si descubrís un error de hace 2 meses, ya no hay respaldo para restaurar.
- El sistema crea un respaldo **al cerrar sesión** automáticamente — por eso conviene **Cerrar sesión** en lugar de cerrar la ventana del programa con la X.

## Pantallas relacionadas

- **`FrmSistema`** — desde donde se abre esta pantalla.
- **`FrmLogAuditoria`** — registra cada respaldo (acción `BACKUP_MANUAL` o `BACKUP_AUTO`).
