namespace AuthService.Entities;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
