namespace TalentManagement.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool Activo { get; set; } = true;
}
