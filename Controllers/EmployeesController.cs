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
            Rola = employee.Rola.ToString(),
            Dzial = employee.Dzial.Nazwa
        };

        return Created($"/employees/{employee.Id}", response);
    }
    [HttpGet]
    [ProducesResponseType<IEnumerable<EmployeeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var employees = await dbContext.Pracownicy
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                Imie = e.Imie,
                Nazwisko = e.Nazwisko,
                Rola = e.Rola.ToString(),
                Dzial = e.Dzial.Nazwa,
                Stanowisko = e.Stanowisko.Nazwa,
                Firma = e.Dzial.Firma.Nazwa
            })
            .ToListAsync(cancellationToken);

        return Ok(employees);
    }
    [HttpGet("{id}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Pracownicy
            .Where(e => e.Id == id)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                Imie = e.Imie,
                Nazwisko = e.Nazwisko,
                Rola = e.Rola.ToString(),
                Dzial =  e.Dzial.Nazwa,
                Stanowisko = e.Stanowisko.Nazwa,
                Firma = e.Dzial.Firma.Nazwa
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee == null)
        {
            return NotFound(new { message = "Pracownik nie został znaleziony." });
        }

        return Ok(employee);
    }
    [HttpPut("{id}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Update(long id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Pracownicy.FindAsync(new object[] { id }, cancellationToken);

        if (employee == null)
        {
            return NotFound(new { message = "Pracownik nie został znaleziony." });
        }

       
        if (employee.Login != request.Login)
        {
            var loginExists = await dbContext.Pracownicy
                .AnyAsync(p => p.Login == request.Login, cancellationToken);

            if (loginExists)
            {
                return Conflict(new { message = "Pracownik o podanym loginie juz istnieje." });
            }
        }
        
        
        employee.Imie = request.Imie;
        employee.Nazwisko = request.Nazwisko;
        employee.Login = request.Login;
        
 
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            employee.HasloHash = passwordHasher.HashPassword(employee, request.Password);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new EmployeeResponse
        {
            Id = employee.Id,
            Imie = employee.Imie,
            Nazwisko = employee.Nazwisko,
        };

        return Ok(response);
    }
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Pracownicy.FindAsync(new object[] { id }, cancellationToken);

        if (employee == null)
        {
            return NotFound(new { message = "Pracownik nie został znaleziony." });
        }
     
        dbContext.Pracownicy.Remove(employee);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent(); 
    }
}
