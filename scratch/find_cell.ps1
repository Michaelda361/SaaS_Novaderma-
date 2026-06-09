$zipPath = "prueba excel.xlsx"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)

$sharedStrings = @()
$sharedStringsEntry = $zip.Entries | Where-Object { $_.FullName -eq 'xl/sharedStrings.xml' }
if ($sharedStringsEntry) {
    $sStream = $sharedStringsEntry.Open()
    $sReader = New-Object System.IO.StreamReader($sStream)
    $sXml = [xml]$sReader.ReadToEnd()
    $sReader.Close()
    foreach ($si in $sXml.sst.si) {
        $text = ""
        if ($si.t) {
            if ($si.t -is [System.String]) {
                $text = $si.t
            } elseif ($si.t.'#text') {
                $text = $si.t.'#text'
            } else {
                $text = $si.t.InnerText
            }
        } elseif ($si.r) {
            $text = ($si.r | ForEach-Object { 
                if ($_.t -is [System.String]) { $_.t } else { $_.t.'#text' }
            }) -join ''
        }
        $sharedStrings += $text
    }
}

$targetIndex = -1
for ($i = 0; $i -lt $sharedStrings.Count; $i++) {
    if ($sharedStrings[$i] -eq "BATCH RECORD PRODUCTO TERMINADO") {
        $targetIndex = $i
        Write-Output "Found target string at shared string index: $i"
        break
    }
}

if ($targetIndex -ne -1) {
    $sheetEntry = $zip.Entries | Where-Object { $_.FullName -eq 'xl/worksheets/sheet1.xml' }
    if ($sheetEntry) {
        $stream = $sheetEntry.Open()
        $reader = New-Object System.IO.StreamReader($stream)
        $xml = [xml]$reader.ReadToEnd()
        $reader.Close()

        $cells = $xml.worksheet.sheetData.row.c | Where-Object { $_.t -eq "s" -and $_.v -eq "$targetIndex" }
        foreach ($c in $cells) {
            # Find the parent row number
            $rowNum = $xml.worksheet.sheetData.row | Where-Object { $_.c -contains $c } | ForEach-Object { $_.r }
            Write-Output "Found cell reference: $($c.r)"
        }
    }
} else {
    Write-Output "Target string not found in shared strings list."
}

$zip.Dispose()
