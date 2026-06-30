<#
.SYNOPSIS
    Script de despliegue mínimo para la aplicación Talent Management.
.DESCRIPTION
    Realiza la compilación (dotnet publish) en modo Release, genera un respaldo
    de la versión anterior en C:\inetpub\novaderma-backup y copia los nuevos archivos
    al directorio de IIS, reiniciando el Application Pool correspondiente.
#>

$ErrorActionPreference = "Stop"

# Configuración
$projectName = "TalentManagement"
$projectPath = "src/Server/TalentManagement.Server.csproj"
$targetDir = "C:\inetpub\novaderma"
$backupDir = "C:\inetpub\novaderma-backup"
$tempPublishDir = "C:\ProgramData\TalentManagement\temp-publish"
$appPoolName = "TalentManagementPool"

Write-Host "=== INICIANDO DESPLIEGUE DE $projectName ===" -ForegroundColor Cyan

# 1. Validar privilegios de administrador
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Este script requiere ejecutarse con privilegios de Administrador para detener/iniciar servicios de IIS."
    exit 1
}

# Importar el módulo web administration
Import-Module WebAdministration -ErrorAction SilentlyContinue

# 2. Compilar y publicar en directorio temporal
Write-Host "[1/6] Compilando y publicando en modo Release..." -ForegroundColor Green
if (Test-Path $tempPublishDir) {
    Remove-Item -Path $tempPublishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempPublishDir -Force | Out-Null

dotnet publish $projectPath -c Release -o $tempPublishDir -r win-x64 --self-contained false

# 3. Detener el Pool de Aplicaciones de IIS para desbloquear archivos
Write-Host "[2/6] Deteniendo el Application Pool '$appPoolName'..." -ForegroundColor Green
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
} else {
    Write-Host "Advertencia: El Application Pool '$appPoolName' no existe en IIS o no se pudo consultar. Se continuará con el despliegue." -ForegroundColor Yellow
}

# 4. Crear backup de la versión anterior
Write-Host "[3/6] Respaldando versión actual en '$backupDir'..." -ForegroundColor Green
if (Test-Path $targetDir) {
    if (Test-Path $backupDir) {
        Write-Host "Limpiando respaldo anterior..." -ForegroundColor Gray
        Remove-Item -Path $backupDir -Recurse -Force
    }
    # Copiar recursivamente
    Copy-Item -Path $targetDir -Destination $backupDir -Recurse -Force
    Write-Host "Respaldo completado con éxito." -ForegroundColor Gray
} else {
    Write-Host "No existe una versión previa en '$targetDir'. Se omitirá el respaldo." -ForegroundColor Yellow
}

# 5. Copiar los archivos nuevos al directorio de destino
Write-Host "[4/6] Copiando nuevos archivos al directorio IIS '$targetDir'..." -ForegroundColor Green
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
} else {
    # Limpiar archivos viejos (excepto uploads o carpetas de datos dinámicos)
    # Por seguridad, no borramos la carpeta 'wwwroot/uploads' si existe localmente
    Get-ChildItem -Path $targetDir | Where-Object { $_.Name -ne "wwwroot" } | Remove-Item -Recurse -Force
    if (Test-Path "$targetDir\wwwroot") {
        Get-ChildItem -Path "$targetDir\wwwroot" | Where-Object { $_.Name -ne "uploads" } | Remove-Item -Recurse -Force
    }
}

# Copiar desde la carpeta temporal al destino
Copy-Item -Path "$tempPublishDir\*" -Destination $targetDir -Recurse -Force

# 6. Iniciar el Application Pool de IIS
Write-Host "[5/6] Iniciando el Application Pool '$appPoolName'..." -ForegroundColor Green
if (Get-WebAppPoolState -Name $appPoolName -ErrorAction SilentlyContinue) {
    Start-WebAppPool -Name $appPoolName
    Write-Host "Application Pool '$appPoolName' iniciado exitosamente."
}

# 7. Limpieza temporal
Write-Host "[6/6] Limpiando archivos temporales..." -ForegroundColor Green
if (Test-Path $tempPublishDir) {
    Remove-Item -Path $tempPublishDir -Recurse -Force
}

# 8. Validar health check
Write-Host "Esperando que la aplicación inicie (5 segundos)..." -ForegroundColor Gray
Start-Sleep -Seconds 5
try {
    $response = Invoke-WebRequest -Uri "http://localhost/health" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Health check OK — La aplicación está respondiendo correctamente." -ForegroundColor Green
    } else {
        Write-Warning "Health check retornó estado HTTP $($response.StatusCode). Verifica los logs en C:\ProgramData\TalentManagement\logs\"
    }
} catch {
    Write-Warning "No se pudo alcanzar /health: $($_.Exception.Message)"
    Write-Warning "Verifica manualmente: http://localhost/health"
    Write-Warning "Revisa los logs en: C:\ProgramData\TalentManagement\logs\"
}

Write-Host "=== DESPLIEGUE FINALIZADO CON ÉXITO ===" -ForegroundColor Cyan
