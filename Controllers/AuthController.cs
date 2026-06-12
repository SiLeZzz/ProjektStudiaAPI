using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    IJwtTokenService jwtTokenService) : ApiControllerBase
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
            return UnauthorizedProblem("Nieprawidlowy login lub haslo.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(employee, employee.HasloHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return UnauthorizedProblem("Nieprawidlowy login lub haslo.");
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

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<AuthenticatedUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedUserDto>> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdValue, out var userId))
        {
            return UnauthorizedProblem("Token uwierzytelniajacy jest niepoprawny.");
        }

        var employee = await dbContext.Pracownicy
            .AsNoTracking()
            .Where(p => p.Id == userId && p.CzyAktywny)
            .Select(p => new AuthenticatedUserDto
            {
                Id = p.Id,
                Login = p.Login,
                Rola = p.Rola.ToString()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return employee is null
            ? UnauthorizedProblem("Uzytkownik przypisany do tokenu nie istnieje albo jest nieaktywny.")
            : Ok(employee);
    }
}
