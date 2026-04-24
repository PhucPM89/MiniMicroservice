using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.Users;

public sealed class UpdateUserRequest
{
    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MinLength(8)]
    [MaxLength(100)]
    public string? Password { get; set; }

    public bool? IsActive { get; set; }
    public List<string>? RoleNames { get; set; }
    public List<string>? GrantedPermissionCodes { get; set; }
    public List<string>? DeniedPermissionCodes { get; set; }
}
