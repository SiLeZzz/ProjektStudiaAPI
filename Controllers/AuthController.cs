using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Contracts.Auth;
using WebAPI.Data;
using WebAPI.Domain.Entities;
using WebAPI.Services.Auth;

namespace WebAPI.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    AppDbContext dbContext,
    IPasswordHasher<Pracownik> passwordHasher,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Pracownicy
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Login == request.Login, cancellationToken);

        if (employee is null || !employee.CzyAktywny)
        {
            return Unauthorized(new { message = "Nieprawidlowy login lub haslo." });
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(employee, employee.HasloHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Nieprawidlowy login lub haslo." });
        }

        var token = jwtTokenService.GenerateToken(employee);

        return Ok(new LoginResponse
        {
            Token = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = new AuthenticatedUserDto
            {
                Id = employee.Id,
                Login = employee.Login,
                Rola = employee.Rola.ToString()
            }
        });
    }
}
