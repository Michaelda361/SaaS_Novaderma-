<#
.SYNOPSIS
    Script de configuración inicial del servidor Windows Server 2019 para TalentManagement.
.DESCRIPTION
    Automatiza la configuración de IIS, directorios, permisos y estructura necesaria
    para hospedar la aplicación TalentManagement en Windows Server 2019.
    EJECUTAR COMO ADMINISTRADOR UNA SOLA VEZ antes del primer despliegue.
.NOTES
    Prerequisito: .NET 10 Hosting Bundle ya debe estar instalado.
    Prerequisito: SQL Server accesible desde este servidor.
#>

$ErrorActionPreference = "Stop"

# ──────────────────────────────────────────────
# CONFIGURACIÓN — Ajusta estos valores
# ──────────────────────────────────────────────
$appPoolName    = "TalentManagementPool"
$siteName       = "TalentManagement"
$siteRoot       = "C:\inetpub\novaderma"
$backupDir      = "C:\inetpub\novaderma-backup"
$dataDir        = "C:\ProgramData\TalentManagement"
$logsDir        = "$dataDir\logs"
$tempDir        = "$dataDir\temp-publish"
$sitePort       = 80          # Cambia a 443 si usas HTTPS directo
$siteBinding    = "*:${sitePort}:"   # Ejemplo: "*:80:" o "*:80:tu.dominio.com"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   SETUP INICIAL — TalentManagement / Novaderma       ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── PASO 0: Validar Administrador ──────────────────────────────
$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Ejecuta este script como Administrador."
    exit 1
}

# ── PASO 1: Validar .NET 10 ────────────────────────────────────
Write-Host "[1/7] Verificando .NET 10..." -ForegroundColor Green
try {
    $dotnetVersion = & dotnet --version 2>&1
    if ($dotnetVersion -notmatch "^10\.") {
        Write-Warning "Se detectó .NET versión '$dotnetVersion'. Se requiere .NET 10."
        Write-Warning "Descarga el Hosting Bundle desde: https://dotnet.microsoft.com/download/dotnet/10.0"
        $resp = Read-Host "¿Continuar de todas formas? (s/N)"
        if ($resp -ne "s") { exit 1 }
    } else {
        Write-Host "   .NET $dotnetVersion detectado. ✓" -ForegroundColor Gray
    }
} catch {
    Write-Error ".NET no está instalado o no está en el PATH. Instala el .NET 10 Hosting Bundle primero."
    exit 1
}

# ── PASO 2: Instalar / Habilitar IIS ──────────────────────────
Write-Host "[2/7] Habilitando características de IIS..." -ForegroundColor Green

$iisFeatures = @(
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-CommonHttpFeatures",
    "IIS-StaticContent",
    "IIS-DefaultDocument",
    "IIS-HttpErrors",
    "IIS-ApplicationDevelopment",
    "IIS-NetFxExtensibility45",
    "IIS-ASPNET45",
    "IIS-ISAPIExtensions",
    "IIS-ISAPIFilter",
    "IIS-WebServerManagementTools",
    "IIS-ManagementConsole",
    "IIS-HttpCompressionDynamic",
    "IIS-HttpCompressionStatic",
    "IIS-Security",
    "IIS-RequestFiltering",
    "IIS-HttpLogging",
    "IIS-HttpRedirect",
    "NetFx4Extended-ASPNET45"
)

foreach ($feature in $iisFeatures) {
    $state = (Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue).State
    if ($state -ne "Enabled") {
        Write-Host "   Habilitando $feature..." -ForegroundColor Gray
        Enable-WindowsOptionalFeature -Online -FeatureName $feature -NoRestart -ErrorAction SilentlyContinue | Out-Null
    }
}
Write-Host "   IIS configurado. ✓" -ForegroundColor Gray

# ── PASO 3: Importar módulo WebAdministration ──────────────────
Import-Module WebAdministration -ErrorAction Stop

# ── PASO 4: Crear directorios ──────────────────────────────────
Write-Host "[3/7] Creando estructura de directorios..." -ForegroundColor Green

$dirs = @($siteRoot, $backupDir, $dataDir, $logsDir, $tempDir)
foreach ($dir in $dirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "   Creado: $dir" -ForegroundColor Gray
    } else {
        Write-Host "   Ya existe: $dir" -ForegroundColor DarkGray
    }
}

# ── PASO 5: Crear Application Pool ────────────────────────────
Write-Host "[4/7] Configurando Application Pool '$appPoolName'..." -ForegroundColor Green

if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName | Out-Null
    Write-Host "   App Pool creado." -ForegroundColor Gray
} else {
    Write-Host "   App Pool ya existe, actualizando configuración..." -ForegroundColor DarkGray
}

# Configurar el App Pool
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedPipelineMode"   -Value "Integrated"
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "startMode"             -Value "AlwaysRunning"
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.idleTimeout" -Value "00:00:00"
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "recycling.periodicRestart.time" -Value "00:00:00"

Write-Host "   App Pool configurado (No Managed Code, AlwaysRunning). ✓" -ForegroundColor Gray

# ── PASO 6: Crear Sitio Web en IIS ────────────────────────────
Write-Host "[5/7] Configurando sitio web '$siteName' en IIS..." -ForegroundColor Green

if (Test-Path "IIS:\Sites\$siteName") {
    Write-Host "   El sitio '$siteName' ya existe. Actualizando ruta..." -ForegroundColor DarkGray
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "physicalPath" -Value $siteRoot
} else {
    # Eliminar Default Web Site en el mismo puerto si existe
    if (Test-Path "IIS:\Sites\Default Web Site") {
        $defaultSite = Get-WebSite -Name "Default Web Site" -ErrorAction SilentlyContinue
        if ($defaultSite -and $defaultSite.Bindings.Collection.bindingInformation -contains $siteBinding) {
            Write-Host "   Eliminando Default Web Site para liberar puerto $sitePort..." -ForegroundColor Yellow
            Remove-Website -Name "Default Web Site"
        }
    }
    New-Website -Name $siteName `
                -PhysicalPath $siteRoot `
                -ApplicationPool $appPoolName `
                -Port $sitePort `
                -Force | Out-Null
    Write-Host "   Sitio '$siteName' creado en puerto $sitePort. ✓" -ForegroundColor Gray
}

# Asignar el App Pool al sitio
Set-ItemProperty "IIS:\Sites\$siteName" -Name "applicationPool" -Value $appPoolName

# ── PASO 7: Permisos de carpetas para el App Pool ──────────────
Write-Host "[6/7] Configurando permisos de carpetas..." -ForegroundColor Green

$appPoolAccount = "IIS AppPool\$appPoolName"
$foldersToGrant = @($siteRoot, $dataDir)

foreach ($folder in $foldersToGrant) {
    try {
        $acl  = Get-Acl $folder
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $appPoolAccount,
            "Modify",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.SetAccessRule($rule)
        Set-Acl -Path $folder -AclObject $acl
        Write-Host "   Permisos 'Modify' otorgados en: $folder" -ForegroundColor Gray
    } catch {
        Write-Warning "No se pudieron configurar permisos en '$folder': $($_.Exception.Message)"
    }
}

# ── PASO 8: Crear appsettings.Production.json de plantilla ────
Write-Host "[7/7] Creando plantilla de configuración de producción..." -ForegroundColor Green

$configPath = "$dataDir\appsettings.Production.json"
if (-not (Test-Path $configPath)) {
    $template = @'
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "REEMPLAZA_CON_TU_TENANT_ID",
    "ClientId": "60ea78c7-4add-4d9e-ba23-ac5d2c1ee4ac",
    "Audience": "api://60ea78c7-4add-4d9e-ba23-ac5d2c1ee4ac",
    "ValidateIssuer": false
  },
  "CorsOrigins": "https://REEMPLAZA_CON_TU_DOMINIO.com",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TalentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "SharePoint": {
    "TenantId": "REEMPLAZA_CON_TU_TENANT_ID",
    "ClientId": "REEMPLAZA_CON_TU_SP_CLIENT_ID",
    "ClientSecret": "REEMPLAZA_CON_TU_SP_SECRET",
    "SiteId": "REEMPLAZA_CON_TU_SITE_ID",
    "BibliotecaDocumentos": "Documentos"
  },
  "AzureStorage": {
    "ConnectionString": "REEMPLAZA_CON_TU_AZURE_STORAGE_CONNECTION_STRING"
  },
  "SyncfusionLicense": "REEMPLAZA_CON_TU_LICENCIA_SYNCFUSION",
  "DatabaseSettings": {
    "UseEnsureCreated": false
  },
  "AllowedHosts": "*"
}
'@
    Set-Content -Path $configPath -Value $template -Encoding UTF8
    Write-Host "   Plantilla creada en: $configPath" -ForegroundColor Yellow
    Write-Host "   ⚠️  IMPORTANTE: Edita ese archivo con tus valores reales antes de desplegar." -ForegroundColor Yellow
} else {
    Write-Host "   Ya existe: $configPath" -ForegroundColor DarkGray
}

# ── RESUMEN FINAL ──────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   SETUP COMPLETADO                                    ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Próximos pasos manuales:" -ForegroundColor White
Write-Host "  1. Edita el archivo de configuración:" -ForegroundColor Yellow
Write-Host "     $configPath" -ForegroundColor Cyan
Write-Host "  2. Asegúrate de que SQL Server esté corriendo y accesible." -ForegroundColor Yellow
Write-Host "  3. Otorga permisos en SQL Server a 'IIS AppPool\$appPoolName'." -ForegroundColor Yellow
Write-Host "  4. (Opcional) Instala LibreOffice para conversión DOCX→PDF." -ForegroundColor Yellow
Write-Host "  5. Ejecuta deploy.ps1 desde el directorio raíz del proyecto." -ForegroundColor Yellow
Write-Host ""
Write-Host "Verificación rápida:" -ForegroundColor White
Write-Host "  iisreset" -ForegroundColor DarkCyan
Write-Host "  Invoke-WebRequest http://localhost/health" -ForegroundColor DarkCyan
Write-Host ""
