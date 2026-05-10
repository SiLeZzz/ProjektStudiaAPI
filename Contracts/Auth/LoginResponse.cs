namespace WebAPI.Contracts.Auth;

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public AuthenticatedUserDto User { get; set; } = null!;
}
