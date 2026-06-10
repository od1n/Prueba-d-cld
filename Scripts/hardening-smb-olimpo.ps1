# Hardening SMB — Ejecutar en OLIMPO como Administrador
# Fix H2: Habilitar cifrado SMB
# Fix M1: Restringir permisos del share

Write-Host "=== Hardening SMB para SistemaGestion ===" -ForegroundColor Cyan

# 1. Habilitar cifrado y firma SMB
Write-Host "`n[1/4] Configurando cifrado y firma SMB..."
Set-SmbServerConfiguration -EncryptData $true -RequireSecuritySignature $true -Force
Write-Host "  OK: Cifrado y firma habilitados." -ForegroundColor Green

# 2. Verificar estado
Write-Host "`n[2/4] Estado actual:"
Get-SmbServerConfiguration | Select-Object EncryptData, RequireSecuritySignature | Format-Table

# 3. Restringir permisos del share (solo la cuenta de SERVO)
Write-Host "[3/4] Ajustando permisos del share SistemaGestion..."
$shareName = "SistemaGestion"
$share = Get-SmbShare -Name $shareName -ErrorAction SilentlyContinue
if ($share) {
    # Remover Everyone si existe
    $permisos = Get-SmbShareAccess -Name $shareName
    foreach ($p in $permisos) {
        if ($p.AccountName -match "Everyone|Todos") {
            Revoke-SmbShareAccess -Name $shareName -AccountName $p.AccountName -Force
            Write-Host "  Removido: $($p.AccountName)" -ForegroundColor Yellow
        }
    }
    # Agregar solo las cuentas necesarias (ajustar nombres segun red)
    # SERVO accede como naos (o la cuenta de maquina SERVO$)
    Grant-SmbShareAccess -Name $shareName -AccountName "PC" -AccessRight Full -Force | Out-Null
    Write-Host "  OK: Permisos restringidos." -ForegroundColor Green
    Write-Host "`n  Permisos actuales:"
    Get-SmbShareAccess -Name $shareName | Format-Table AccountName, AccessRight
    Write-Host "  NOTA: Agrega la cuenta de SERVO manualmente si es necesario:"
    Write-Host "  Grant-SmbShareAccess -Name SistemaGestion -AccountName 'SERVO\naos' -AccessRight Change -Force"
} else {
    Write-Host "  WARN: Share '$shareName' no encontrado." -ForegroundColor Yellow
}

# 4. Verificar firewall
Write-Host "[4/4] Reglas de firewall recomendadas:"
Write-Host "  netsh advfirewall firewall add rule name='SG-API-HTTP' dir=in action=allow protocol=tcp localport=5050 remoteip=192.168.68.0/24"
Write-Host "  netsh advfirewall firewall add rule name='SG-API-HTTPS' dir=in action=allow protocol=tcp localport=5443 remoteip=192.168.68.0/24"
Write-Host "  netsh advfirewall firewall add rule name='SG-Digitalizador' dir=in action=allow protocol=tcp localport=8080 remoteip=192.168.68.0/24"
Write-Host ""
Write-Host "=== Hardening completo ===" -ForegroundColor Cyan
