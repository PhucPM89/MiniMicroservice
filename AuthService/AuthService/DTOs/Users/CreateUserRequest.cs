using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.Users;

public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public List<string> RoleNames { get; set; } = [];
    public List<string> GrantedPermissionCodes { get; set; } = [];
    public List<string> DeniedPermissionCodes { get; set; } = [];
}
