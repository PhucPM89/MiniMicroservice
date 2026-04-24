namespace AuthService.DTOs.Users;

public sealed class UserQueryParameters
{
    public string? Role { get; set; }
    public string? Cursor { get; set; }
    public int PageSize { get; set; } = 20;
}
