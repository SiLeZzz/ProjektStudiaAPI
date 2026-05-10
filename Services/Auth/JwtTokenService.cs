using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Configuration;
using WebAPI.Domain.Entities;

namespace WebAPI.Services.Auth;

public class JwtTokenService(IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public TokenResult GenerateToken(Pracownik employee)
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(_jwtOptions.ExpirationHours);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name, employee.Login),
            new(ClaimTypes.Role, employee.Rola.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new TokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
