namespace AuthService.Entities;

public sealed class UserPermission
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsGranted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
