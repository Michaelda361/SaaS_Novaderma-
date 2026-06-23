<#
.SYNOPSIS
    Script de rollback rápido para la aplicación Talent Management.
.DESCRIPTION
    Restaura la versión previa de la aplicación desde C:\inetpub\novaderma-backup
    reemplazando la versión activa y reiniciando el Application Pool.
#>

$ErrorActionPreference = "Stop"

# Configuración
$projectName = "TalentManagement"
$targetDir = "C:\inetpub\novaderma"
$backupDir = "C:\inetpub\novaderma-backup"
$failedDir = "C:\inetpub\novaderma-failed"
$appPoolName = "TalentManagementPool"

Write-Host "=== INICIANDO ROLLBACK DE $projectName ===" -ForegroundColor Red

# 1. Validar privilegios de administrador
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Este script requiere ejecutarse con privilegios de Administrador para interactuar con IIS."
    exit 1
}

# Importar el módulo web administration
Import-Module WebAdministration -ErrorAction SilentlyContinue

# 2. Validar que exista la versión de respaldo
if (-not (Test-Path $backupDir)) {
    Write-Error "Error: No se encontró la carpeta de respaldo '$backupDir'. No es posible realizar rollback."
    exit 1
}

# 3. Detener el Pool de Aplicaciones de IIS para desbloquear archivos
Write-Host "[1/4] Deteniendo el Application Pool '$appPoolName'..." -ForegroundColor Green
if (Get-WebAppPoolState -Name $appPoolName -ErrorAction SilentlyContinue) {
    Stop-WebAppPool -Name $appPoolName
    # Esperar a que se detenga
    $timeout = 15
    $elapsed = 0
    while ((Get-WebAppPoolState -Name $appPoolName).Value -ne "Stopped" -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds 1
        $elapsed++
    }
    Write-Host "Application Pool detenido."
}

# 4. Mover la versión fallida y restaurar el respaldo
Write-Host "[2/4] Intercambiando directorios..." -ForegroundColor Green
if (Test-Path $failedDir) {
    Remove-Item -Path $failedDir -Recurse -Force
}

if (Test-Path $targetDir) {
    # Guardar la versión fallida para inspección
    Move-Item -Path $targetDir -Destination $failedDir -Force
    Write-Host "Versión actual movida a '$failedDir'." -ForegroundColor Gray
}

# Mover/copiar el backup al directorio activo
Copy-Item -Path $backupDir -Destination $targetDir -Recurse -Force
Write-Host "Versión de respaldo restaurada en '$targetDir'." -ForegroundColor Gray

# 5. Iniciar el Application Pool de IIS
Write-Host "[3/4] Iniciando el Application Pool '$appPoolName'..." -ForegroundColor Green
if (Get-WebAppPoolState -Name $appPoolName -ErrorAction SilentlyContinue) {
    Start-WebAppPool -Name $appPoolName
    Write-Host "Application Pool '$appPoolName' iniciado exitosamente."
}

# 6. Validar estado de salud
Write-Host "[4/4] Validando el endpoint de salud local..." -ForegroundColor Green
Start-Sleep -Seconds 3 # Dar tiempo para arrancar
try {
    # Usar localhost o dominio en producción
    $response = Invoke-WebRequest -Uri "http://localhost/health" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "Rollback validado: La aplicación responde OK en el endpoint de salud." -ForegroundColor Green
    } else {
        Write-Host "Advertencia: El endpoint de salud retornó estado $($response.StatusCode)." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Advertencia: No se pudo conectar al endpoint de salud local ($($_.Exception.Message)). Verifique manualmente." -ForegroundColor Yellow
}

Write-Host "=== ROLLBACK FINALIZADO CON ÉXITO ===" -ForegroundColor Cyan
