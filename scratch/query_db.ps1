$connString = "Server=localhost;Database=TalentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Id, Nombre, Apellido, Email, Rol, Activo FROM Colaboradores"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    $id = $reader["Id"]
    $nombre = $reader["Nombre"]
    $apellido = $reader["Apellido"]
    $email = $reader["Email"]
    $rol = $reader["Rol"]
    $activo = $reader["Activo"]
    Write-Output "ID: $id | Name: $nombre $apellido | Email: $email | Role: $rol | Active: $activo"
}
$conn.Close()
