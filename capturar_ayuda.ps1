Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinAPI {
    // CORRECTO: obtener el handle de la ventana de consola via kernel32
    [DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$carpeta = "C:\Users\Toor\source\repos\Prueba d cld\SistemaGestion.Api\wwwroot\img\ayuda"
if (-not (Test-Path $carpeta)) { New-Item -ItemType Directory -Path $carpeta | Out-Null }

$pantallas = @(
    @{ nombre = "menu-principal";      instruccion = "MENU PRINCIPAL: pantalla de inicio con los 6 modulos" },
    @{ nombre = "produccion-menu";     instruccion = "Abre PRODUCCION: el dialogo del modulo (con Kardex, Materia Prima, etc.)" },
    @{ nombre = "kardex-lista";        instruccion = "Navega a KARDEX: lista de items con datos en la grilla" },
    @{ nombre = "kardex-form";         instruccion = "Abre KARDEX: formulario Nuevo Movimiento (visible y al frente)" },
    @{ nombre = "materia-prima-lista"; instruccion = "Navega a MATERIA PRIMA: lista de insumos" },
    @{ nombre = "produccion-registro"; instruccion = "Abre REGISTRO DE PRODUCCION: formulario abierto" },
    @{ nombre = "transacciones-menu";  instruccion = "Abre TRANSACCIONES: dialogo con las 6 opciones" },
    @{ nombre = "facturas-lista";      instruccion = "Navega a FACTURAS: lista con facturas en la grilla" },
    @{ nombre = "factura-form";        instruccion = "Abre NUEVA FACTURA: formulario vacio" },
    @{ nombre = "nomina-menu";         instruccion = "Abre NOMINA: dialogo del modulo (Empleados, Nomina, etc.)" },
    @{ nombre = "empleados-lista";     instruccion = "Navega a EMPLEADOS: lista de empleados" },
    @{ nombre = "nomina-pago-form";    instruccion = "Abre NUEVO PERIODO DE NOMINA: formulario abierto" }
)

# Handle CORRECTO de la consola (kernel32, no Get-Process)
$hConsola = [WinAPI]::GetConsoleWindow()

function Capturar-Pantalla($nombre) {
    # OCULTAR la consola completamente (SW_HIDE = 0, no SW_MINIMIZE)
    [WinAPI]::ShowWindow($hConsola, 0) | Out-Null
    Start-Sleep -Milliseconds 400

    # Buscar y MAXIMIZAR la ventana de SistemaGestion
    $proc = Get-Process | Where-Object {
        $_.MainWindowHandle -ne [IntPtr]::Zero -and
        ($_.MainWindowTitle -like "*Sistema*" -or $_.ProcessName -like "*SistemaGestion*")
    } | Sort-Object -Property CPU -Descending | Select-Object -First 1

    if ($null -ne $proc) {
        [WinAPI]::ShowWindow($proc.MainWindowHandle, 3) | Out-Null   # SW_MAXIMIZE = 3
        [WinAPI]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
        Start-Sleep -Milliseconds 600
    } else {
        Start-Sleep -Milliseconds 400
    }

    # Capturar PANTALLA COMPLETA (SistemaGestion esta maximizado, consola oculta)
    $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp    = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height)
    $g      = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
    $g.Dispose()

    $ruta = Join-Path $carpeta "$nombre.png"
    $bmp.Save($ruta, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()

    # Restaurar la consola
    [WinAPI]::ShowWindow($hConsola, 9) | Out-Null   # SW_RESTORE = 9
    [WinAPI]::SetForegroundWindow($hConsola) | Out-Null
    Start-Sleep -Milliseconds 400

    return $ruta
}

Write-Host ""
Write-Host "=============================================="
Write-Host "  CAPTURADOR v3 - SistemaGestion"
Write-Host "  Consola: OCULTA durante captura"
Write-Host "  SistemaGestion: MAXIMIZADO automaticamente"
Write-Host "=============================================="
Write-Host ""
Write-Host "ANTES DE EMPEZAR:" -ForegroundColor White
Write-Host "  SistemaGestion debe estar abierto e iniciado"
Write-Host "  No hace falta maximizarlo: el script lo hace"
Write-Host ""

$total = $pantallas.Count
for ($i = 0; $i -lt $total; $i++) {
    $p = $pantallas[$i]
    Write-Host "----------------------------------------------"
    Write-Host "  [$($i+1)/$total] $($p.instruccion)" -ForegroundColor Yellow
    Write-Host "  Navega a esa pantalla y presiona ENTER..." -ForegroundColor Cyan
    Read-Host

    $ruta = Capturar-Pantalla $p.nombre
    Write-Host "  OK: $(Split-Path $ruta -Leaf)" -ForegroundColor Green
    Write-Host ""
}

Write-Host "=============================================="
Write-Host "  Listo! $total capturas guardadas en:"
Write-Host "  $carpeta"
Write-Host "=============================================="
