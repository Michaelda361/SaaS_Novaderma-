try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=localhost;Database=TalentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;")
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
    SELECT 
        t.NAME AS TableName,
        p.rows AS RowCounts
    FROM 
        sys.tables t
    INNER JOIN      
        sys.indexes i ON t.OBJECT_ID = i.object_id
    INNER JOIN 
        sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
    WHERE 
        t.is_ms_shipped = 0 AND i.index_id <= 1
    ORDER BY 
        RowCounts DESC, t.NAME
"@
    $reader = $cmd.ExecuteReader()
    Write-Output "=== TABLE ROW COUNTS ==="
    while ($reader.Read()) {
        Write-Output "$($reader['TableName']): $($reader['RowCounts'])"
    }
    $reader.Close()
    $conn.Close()
} catch {
    Write-Error $_.Exception.Message
}
