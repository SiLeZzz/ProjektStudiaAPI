namespace WebAPI.Services.Auth;

public class TokenResult
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
}
