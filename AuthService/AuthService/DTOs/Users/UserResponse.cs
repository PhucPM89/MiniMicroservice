namespace AuthService.DTOs.Users;

public sealed class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> EffectivePermissions { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> DirectGrantedPermissions { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> DirectDeniedPermissions { get; set; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
