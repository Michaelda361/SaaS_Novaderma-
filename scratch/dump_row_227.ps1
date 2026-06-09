$zipPath = "prueba excel.xlsx"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
$sheetEntry = $zip.Entries | Where-Object { $_.FullName -eq 'xl/worksheets/sheet1.xml' }
if ($sheetEntry) {
    $stream = $sheetEntry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $xml = [xml]$reader.ReadToEnd()
    $reader.Close()

    # Get shared strings
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
                if ($si.t -is [System.String]) { $text = $si.t } else { $text = $si.t.'#text' }
            } elseif ($si.r) {
                $text = ($si.r | ForEach-Object { 
                    if ($_.t -is [System.String]) { $_.t } else { $_.t.'#text' }
                }) -join ''
            }
            $sharedStrings += $text
        }
    }

    $row = $xml.worksheet.sheetData.row | Where-Object { $_.r -eq "227" }
    if ($row) {
        $cells = @()
        foreach ($c in $row.c) {
            $val = ""
            if ($c.v) {
                if ($c.t -eq "s") {
                    $val = $sharedStrings[[int]$c.v]
                } else {
                    $val = $c.v
                }
            }
            $cells += "$($c.r):$val"
        }
        Write-Output "Row 227: $($cells -join ', ')"
    }
}
$zip.Dispose()
