# Sistema de Gestión — C# Windows Forms

Basado en el Menu_Principal de la imagen capturada.

## Estructura del proyecto

```
SistemaGestion/
├── Program.cs                  ← Punto de entrada
├── SistemaGestion.csproj       ← Archivo de proyecto (.NET 6 Windows)
├── MenuPrincipal.cs            ← Ventana principal con los 4 módulos
├── Models/
│   └── DB.cs                   ← Modelos de datos + repositorio en memoria
└── Forms/
    ├── FrmTransacciones.cs     ← CRUD completo de transacciones
    ├── FrmGastos.cs            ← CRUD completo de gastos
    └── FrmOtros.cs             ← Reportes, Archivos del sistema, Configuración
```

## Módulos implementados

| Módulo               | Funcionalidad                                                    |
|----------------------|------------------------------------------------------------------|
| **Reportes**         | 3 pestañas: Resumen con tarjetas KPI, lista transacciones, lista gastos |
| **Archivos del sistema** | Tabla de archivos, agregar (OpenFileDialog), eliminar, crear respaldo |
| **Transacciones**    | CRUD completo: nueva, editar, anular; balance en tiempo real     |
| **Gastos**           | CRUD completo: nuevo, editar, eliminar; totales por estado       |
| **Configuración**    | Nombre sistema, versión, empresa, moneda, respaldo automático   |

## Cómo ejecutar

### Requisitos
- .NET 6 SDK (Windows) → https://dotnet.microsoft.com/download/dotnet/6.0
- Visual Studio 2022 **o** solo el SDK para usar la línea de comandos

### Con Visual Studio
1. Abrir `SistemaGestion.csproj`
2. Presionar **F5**

### Con línea de comandos
```bash
cd SistemaGestion
dotnet run
```

## Notas
- Los datos son **en memoria** (se pierden al cerrar). Para persistencia
  real, integrar con SQLite usando `Microsoft.Data.Sqlite` o SQL Server.
- Los íconos de la imagen original (estilo MS Access) se simplificaron
  a botones con emoji y colores corporativos.
