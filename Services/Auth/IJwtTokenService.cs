using WebAPI.Domain.Entities;

namespace WebAPI.Services.Auth;

public interface IJwtTokenService
{
    TokenResult GenerateToken(Pracownik employee);
}
