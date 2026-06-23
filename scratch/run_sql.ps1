try {
    $connString = "Server=localhost;Database=TalentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $conn.Open()
    
    $sql = Get-Content -Raw -Path "scratch\update_users.sql"
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $rowsAffected = $cmd.ExecuteNonQuery()
    
    Write-Output "SQL script executed successfully. Rows affected: $rowsAffected"
    $conn.Close()
} catch {
    Write-Error $_.Exception.Message
}
