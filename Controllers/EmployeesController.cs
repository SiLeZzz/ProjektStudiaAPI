using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Contracts.Employees;
using WebAPI.Data;
using WebAPI.Domain.Entities;

namespace WebAPI.Controllers;

[ApiController]
[Route("employees")]
[Authorize(Roles = nameof(EmployeeRole.Administrator))]
public class EmployeesController(
    AppDbContext dbContext,
    IPasswordHasher<Pracownik> passwordHasher) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var loginExists = await dbContext.Pracownicy
            .AnyAsync(p => p.Login == request.Login, cancellationToken);

        if (loginExists)
        {
            return Conflict(new { message = "Pracownik o podanym loginie juz istnieje." });
        }

        var dzialExists = await dbContext.Dzialy
            .AnyAsync(d => d.Id == request.DzialId, cancellationToken);

        if (!dzialExists)
        {
            ModelState.AddModelError(nameof(request.DzialId), "Podany dzial nie istnieje.");
            return ValidationProblem(ModelState);
        }

        var stanowiskoExists = await dbContext.Stanowiska
            .AnyAsync(s => s.Id == request.StanowiskoId, cancellationToken);

        if (!stanowiskoExists)
        {
            ModelState.AddModelError(nameof(request.StanowiskoId), "Podane stanowisko nie istnieje.");
            return ValidationProblem(ModelState);
        }

        var employee = new Pracownik
        {
            Imie = request.Imie,
            Nazwisko = request.Nazwisko,
            Login = request.Login,
            Rola = request.Rola,
            CzyAktywny = true,
            DzialId = request.DzialId,
            StanowiskoId = request.StanowiskoId
        };

        employee.HasloHash = passwordHasher.HashPassword(employee, request.Password);

        dbContext.Pracownicy.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new EmployeeResponse
        {
            Id = employee.Id,
            Imie = employee.Imie,
            Nazwisko = employee.Nazwisko,
            Login = employee.Login,
            Rola = employee.Rola.ToString(),
            DzialId = employee.DzialId,
            StanowiskoId = employee.StanowiskoId,
            CzyAktywny = employee.CzyAktywny
        };

        return Created($"/employees/{employee.Id}", response);
    }
}
