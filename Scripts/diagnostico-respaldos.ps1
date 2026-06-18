# ============================================================
# DIAGNOSTICO DE RESPALDOS — Ejecutar en OLIMPO como usuario PC
# ============================================================

Write-Host "=== 1. FECHA DE sg.key vs RESPALDOS ===" -ForegroundColor Cyan
$keyPath = "$env:APPDATA\SistemaGestion\sg.key"
if (Test-Path $keyPath) {
    $keyInfo = Get-Item $keyPath
    Write-Host "sg.key:"
    Write-Host "  Creado:     $($keyInfo.CreationTime)"
    Write-Host "  Modificado: $($keyInfo.LastWriteTime)"
    Write-Host "  Tamano:     $($keyInfo.Length) bytes"
} else {
    Write-Host "sg.key NO EXISTE en $keyPath" -ForegroundColor Red
}

Write-Host ""
Write-Host "Respaldos .enc:" -ForegroundColor Yellow
$backupDir = "$env:APPDATA\SistemaGestion\Respaldos"
if (Test-Path $backupDir) {
    Get-ChildItem "$backupDir\*.db.enc" | Sort-Object CreationTime | ForEach-Object {
        Write-Host "  $($_.Name)  Creado: $($_.CreationTime)  Tamano: $($_.Length)"
    }
} else {
    Write-Host "  Carpeta de respaldos no existe" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 2. BUSCAR BD SIN CIFRAR EN TODO EL DISCO ===" -ForegroundColor Cyan
Write-Host "(esto puede tardar unos segundos...)"
$found = Get-ChildItem -Path C:\ -Filter "sistema_gestion*.db" -Recurse -ErrorAction SilentlyContinue 2>$null
if ($found) {
    $found | ForEach-Object {
        Write-Host "  $($_.FullName)  Tamano: $($_.Length)  Modificado: $($_.LastWriteTime)" -ForegroundColor Green
    }
} else {
    Write-Host "  No se encontro ninguna copia .db sin cifrar" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== 3. PAPELERA DE RECICLAJE ===" -ForegroundColor Cyan
$shell = New-Object -ComObject Shell.Application
$recycleBin = $shell.NameSpace(0x0a)
$dbItems = $recycleBin.Items() | Where-Object { $_.Name -like "*sistema_gestion*" -or $_.Name -like "*.db" -or $_.Name -like "*sg.key*" }
if ($dbItems) {
    $dbItems | ForEach-Object {
        Write-Host "  $($_.Name)  Tamano: $($_.Size)  Eliminado: $($_.ExtendedProperty('System.Recycle.DateDeleted'))" -ForegroundColor Green
    }
} else {
    Write-Host "  No hay archivos relevantes en la papelera" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== 4. SHADOW COPIES (puntos de restauracion) ===" -ForegroundColor Cyan
$shadows = vssadmin list shadows 2>&1
if ($shadows -match "No items found") {
    Write-Host "  No hay Shadow Copies disponibles" -ForegroundColor Yellow
} else {
    Write-Host $shadows
    # Intentar acceder a la BD en el shadow copy mas reciente
    $shadowPath = ($shadows | Select-String "Shadow Copy Volume: (\\\\.*?$)" | Select-Object -Last 1)
    if ($shadowPath) {
        $vol = $shadowPath.Matches[0].Groups[1].Value
        $testPath = "${vol}\SistemaGestion\sistema_gestion.db"
        Write-Host ""
        Write-Host "  Probando: $testPath"
        if (Test-Path $testPath) {
            $fi = Get-Item $testPath
            Write-Host "  ENCONTRADA: $($fi.Length) bytes, modificada $($fi.LastWriteTime)" -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "=== 5. CONTENIDO DE AppData\SistemaGestion ===" -ForegroundColor Cyan
$sgDir = "$env:APPDATA\SistemaGestion"
if (Test-Path $sgDir) {
    Get-ChildItem $sgDir -Recurse | ForEach-Object {
        $rel = $_.FullName.Substring($sgDir.Length)
        Write-Host "  $rel  ($($_.Length) bytes)  $($_.LastWriteTime)"
    }
} else {
    Write-Host "  Carpeta no existe" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 6. VERIFICAR TAMANO DE BD ACTUAL ===" -ForegroundColor Cyan
$dbPath = "C:\SistemaGestion\sistema_gestion.db"
if (Test-Path $dbPath) {
    $fi = Get-Item $dbPath
    Write-Host "  $dbPath  Tamano: $($fi.Length) bytes  Modificado: $($fi.LastWriteTime)"

    # Contar registros basicos
    try {
        $dll = "C:\Users\PC\AppData\Local\SistemaGestion\publish\App\Microsoft.Data.Sqlite.dll"
        if (Test-Path $dll) {
            try { Add-Type -Path $dll } catch {}
            $conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath")
            $conn.Open()
            foreach ($tabla in @("Clientes","Productos","Facturas","Usuarios")) {
                $c = $conn.CreateCommand()
                $c.CommandText = "SELECT COUNT(*) FROM $tabla"
                try {
                    $count = $c.ExecuteScalar()
                    Write-Host "    $tabla : $count registros"
                } catch {
                    Write-Host "    $tabla : (tabla no existe)"
                }
            }
            $conn.Close()
        }
    } catch {
        Write-Host "  (no se pudo consultar la BD)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  BD no existe en $dbPath" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== DIAGNOSTICO COMPLETO ===" -ForegroundColor Green
Write-Host "Copia TODA esta salida y pegala en el chat." -ForegroundColor White
