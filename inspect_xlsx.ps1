$path = "prueba excel.xlsx"
if (-not (Test-Path $path)) {
    Write-Output "MISSING"
    exit 1
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Read)
$zip.Entries | ForEach-Object { $_.FullName } | Select-Object -First 100
Write-Output '---'
$shared = ($zip.Entries | Where-Object { $_.FullName -eq 'xl/sharedStrings.xml' })[0]
if ($shared -ne $null) {
    Write-Output 'sharedStrings.xml:'
    $r = $shared.Open()
    $sr = New-Object System.IO.StreamReader($r)
    $xml = [xml]$sr.ReadToEnd()
    $sr.Close()
    $idx = 0
    foreach ($si in $xml.sst.si) {
        if ($si.t) {
            Write-Output "$idx -> $($si.t.'#text')"
        } else {
            $text = ($si.r | ForEach-Object { $_.t.'#text' }) -join ''
            Write-Output "$idx -> $text"
        }
        $idx++
        if ($idx -ge 220) { break }
    }
    Write-Output '---'
}
$sheet = ($zip.Entries | Where-Object { $_.FullName -match '^xl/worksheets/sheet' })[0]
if ($sheet -ne $null) {
    Write-Output $sheet.FullName
    $reader = $sheet.Open()
    $sr = New-Object System.IO.StreamReader($reader)
    for ($i = 0; $i -lt 200 -and -not $sr.EndOfStream; $i++) {
        $line = $sr.ReadLine()
        if ($line -match '<row|<c |<v>|<f>') {
            Write-Output $line
        }
    }
    $sr.Close()
}
$zip.Dispose()
$fs.Close()
