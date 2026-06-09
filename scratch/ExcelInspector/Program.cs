using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Server=localhost;Database=TalentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;";
        Console.WriteLine("Connecting to database: " + connStr);

        try
        {
            using var conn = new SqlConnection(connStr);
            conn.Open();
            Console.WriteLine("Connection successful!\n");

            // Query audit logs for document ID 2090
            string auditQuery = "SELECT Id, EntidadId, EntidadNombre, Accion, ColaboradorNombre, FechaHora, Observaciones FROM AuditLogs WHERE EntidadTipo = 'ControlDocumental' AND EntidadId BETWEEN 2090 AND 2100 ORDER BY Id ASC;";
            using var cmd = new SqlCommand(auditQuery, conn);
            using var reader = cmd.ExecuteReader();
            Console.WriteLine("Audit Logs for documents 2090-2100:");
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                int entidadId = reader.GetInt32(1);
                string nombre = reader.GetString(2);
                string accion = reader.GetString(3);
                string colNombre = reader.GetString(4);
                DateTime fecha = reader.GetDateTime(5);
                string obs = reader.IsDBNull(6) ? "" : reader.GetString(6);

                Console.WriteLine($"ID: {id} | {fecha:yyyy-MM-dd HH:mm:ss} | User: {colNombre} | Action: {accion} | Document ID: {entidadId} - '{nombre}' | Obs: {obs}");
            }
            reader.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
