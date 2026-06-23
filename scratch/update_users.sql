-- Reactivate Michael Ramirez as Admin
UPDATE Colaboradores 
SET Activo = 1, 
    Rol = 'Admin' 
WHERE Id = 5;
