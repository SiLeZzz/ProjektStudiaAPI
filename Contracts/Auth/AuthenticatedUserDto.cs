namespace WebAPI.Contracts.Auth;

public class AuthenticatedUserDto
{
    public long Id { get; set; }
    public string Login { get; set; } = null!;
    public string Rola { get; set; } = null!;
}
