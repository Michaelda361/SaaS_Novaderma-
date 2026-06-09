# Build verification app
Write-Output "Building VerificationApp..."
dotnet build scratch/VerificationApp/VerificationApp.csproj

# Start the TalentManagement server in the background
Write-Output "Starting TalentManagement server..."
$serverProcess = Start-Process dotnet -ArgumentList "run --project src/Server/TalentManagement.Server.csproj --urls http://localhost:5194" -PassThru -NoNewWindow

# Wait a couple of seconds
Start-Sleep -Seconds 5

# Run the verification client
Write-Output "Running VerificationApp..."
$testOutput = dotnet run --project scratch/VerificationApp/VerificationApp.csproj
Write-Output $testOutput

# Stop the server
Write-Output "Stopping TalentManagement server..."
Stop-Process -Id $serverProcess.Id -Force

# Extract the Listado Maestro ID from the test output
$listadoId = $null
foreach ($line in ($testOutput -split "`r`n")) {
    if ($line -match "Created Listado Maestro ID:\s*(\d+)") {
        $listadoId = $Matches[1]
        break
    }
}

if ($listadoId -ne $null) {
    Write-Output "--------------------------------------------------"
    Write-Output "DATABASE VERIFICATION FOR NEW LISTADO ID $listadoId"
    Write-Output "--------------------------------------------------"
    
    # 1. Query number of columns/campos created
    $campos = Invoke-Sqlcmd -Query "SELECT CampoClave, Nombre, Tipo, Activo FROM DocumentoControlCampoDefiniciones WHERE ListadoMaestroId = $listadoId" -Database "TalentManagementDB" -ServerInstance "localhost"
    Write-Output "Campos/Columnas creados en DB:"
    $campos | Format-Table -AutoSize
    
    # 2. Query total documents active
    $activeDocs = Invoke-Sqlcmd -Query "SELECT Count(*) as TotalActivos FROM DocumentosControl WHERE ListadoMaestroId = $listadoId AND Activo = 1" -Database "TalentManagementDB" -ServerInstance "localhost"
    Write-Output "Total Documentos Activos en DB: $($activeDocs.TotalActivos)"

    # 3. Check if 'BATCH RECORD PRODUCTO TERMINADO' is active
    $brDoc = Invoke-Sqlcmd -Query "SELECT Codigo, Nombre, Activo FROM DocumentosControl WHERE ListadoMaestroId = $listadoId AND Nombre = 'BATCH RECORD PRODUCTO TERMINADO'" -Database "TalentManagementDB" -ServerInstance "localhost"
    if ($brDoc -ne $null) {
        Write-Output "Documento 'BATCH RECORD PRODUCTO TERMINADO' estado: Codigo=$($brDoc.Codigo), Activo=$($brDoc.Activo)"
    } else {
        Write-Output "Documento 'BATCH RECORD PRODUCTO TERMINADO' no existe para el nuevo Listado Maestro (CORRECTO)."
    }

    Write-Output "--------------------------------------------------"
} else {
    Write-Output "ERROR: Could not parse Listado Maestro ID from test output."
}
