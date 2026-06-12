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
    IPasswordHasher<Pracownik> passwordHasher) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<EmployeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<EmployeeResponse>>> GetAll(
        [FromQuery] string? number,
        [FromQuery] string? firstName,
        [FromQuery] string? lastName,
        [FromQuery] string? login,
        [FromQuery] long? departmentId,
        [FromQuery] string? department,
        [FromQuery] long? companyId,
        [FromQuery] string? company,
        [FromQuery] long? positionId,
        [FromQuery] string? position,
        [FromQuery] EmployeeRole? role,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Pracownicy
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(number))
        {
            var normalizedNumber = number.Trim();
            if (!long.TryParse(normalizedNumber, out var employeeNumber))
            {
                return Ok(Array.Empty<EmployeeResponse>());
            }

            query = query.Where(e => e.Id == employeeNumber);
        }

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            var normalizedFirstName = firstName.Trim();
            query = query.Where(e => EF.Functions.ILike(e.Imie, $"%{normalizedFirstName}%"));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            var normalizedLastName = lastName.Trim();
            query = query.Where(e => EF.Functions.ILike(e.Nazwisko, $"%{normalizedLastName}%"));
        }

        if (!string.IsNullOrWhiteSpace(login))
        {
            var normalizedLogin = login.Trim();
            query = query.Where(e => EF.Functions.ILike(e.Login, $"%{normalizedLogin}%"));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DzialId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var normalizedDepartment = department.Trim();
            query = query.Where(e => EF.Functions.ILike(e.Dzial.Nazwa, $"%{normalizedDepartment}%"));
        }

        if (companyId.HasValue)
        {
            query = query.Where(e => e.Dzial.FirmaId == companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(company))
        {
            var normalizedCompany = company.Trim();
            query = query.Where(e => EF.Functions.ILike(e.Dzial.Firma.Nazwa, $"%{normalizedCompany}%"));
        }

        if (positionId.HasValue)
        {
            query = query.Where(e => e.StanowiskoId == positionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(position))
        {
            var normalizedPosition = position.Trim();
            query = query.Where(e => EF.Functions.ILike(e.Stanowisko.Nazwa, $"%{normalizedPosition}%"));
        }

        if (role.HasValue)
        {
            query = query.Where(e => e.Rola == role.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(e => e.CzyAktywny == isActive.Value);
        }

        var employees = await query
            .OrderBy(e => e.Nazwisko)
            .ThenBy(e => e.Imie)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                Imie = e.Imie,
                Nazwisko = e.Nazwisko,
                Login = e.Login,
                Rola = e.Rola.ToString(),
                CzyAktywny = e.CzyAktywny,
                DzialId = e.DzialId,
                Dzial = e.Dzial.Nazwa,
                FirmaId = e.Dzial.FirmaId,
                Firma = e.Dzial.Firma.Nazwa,
                StanowiskoId = e.StanowiskoId,
                Stanowisko = e.Stanowisko.Nazwa
            })
            .ToListAsync(cancellationToken);

        return Ok(employees);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var employee = await GetEmployeeResponse(id, cancellationToken);

        return employee is null
            ? NotFoundProblem("Pracownik o podanym identyfikatorze nie istnieje.")
            : Ok(employee);
    }

    [HttpPost]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (!ValidateEmployeeInput(
                request.Imie,
                nameof(request.Imie),
                request.Nazwisko,
                nameof(request.Nazwisko),
                request.Login,
                nameof(request.Login),
                request.Rola,
                request.DzialId,
                request.StanowiskoId))
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            ModelState.AddModelError(nameof(request.Password), "Haslo jest wymagane.");
            return ValidationProblem(ModelState);
        }

        var normalizedLogin = request.Login.Trim();
        var loginExists = await dbContext.Pracownicy
            .AnyAsync(p => p.Login == normalizedLogin, cancellationToken);

        if (loginExists)
        {
            return ConflictProblem("Pracownik o podanym loginie juz istnieje.");
        }

        if (!await DepartmentExists(request.DzialId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.DzialId), "Podany dzial nie istnieje.");
            return ValidationProblem(ModelState);
        }

        if (!await PositionExists(request.StanowiskoId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.StanowiskoId), "Podane stanowisko nie istnieje.");
            return ValidationProblem(ModelState);
        }

        var employee = new Pracownik
        {
            Imie = request.Imie.Trim(),
            Nazwisko = request.Nazwisko.Trim(),
            Login = normalizedLogin,
            Rola = request.Rola,
            CzyAktywny = true,
            DzialId = request.DzialId,
            StanowiskoId = request.StanowiskoId
        };

        employee.HasloHash = passwordHasher.HashPassword(employee, request.Password);

        dbContext.Pracownicy.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetEmployeeResponse(employee.Id, cancellationToken);
        return Created($"/employees/{employee.Id}", response);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Update(
        long id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateEmployeeInput(
                request.Imie,
                nameof(request.Imie),
                request.Nazwisko,
                nameof(request.Nazwisko),
                request.Login,
                nameof(request.Login),
                request.Rola,
                request.DzialId,
                request.StanowiskoId))
        {
            return ValidationProblem(ModelState);
        }

        var employee = await dbContext.Pracownicy
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (employee is null)
        {
            return NotFoundProblem("Pracownik o podanym identyfikatorze nie istnieje.");
        }

        var normalizedLogin = request.Login.Trim();
        if (employee.Login != normalizedLogin)
        {
            var loginExists = await dbContext.Pracownicy
                .AnyAsync(p => p.Id != id && p.Login == normalizedLogin, cancellationToken);

            if (loginExists)
            {
                return ConflictProblem("Pracownik o podanym loginie juz istnieje.");
            }
        }

        if (!await DepartmentExists(request.DzialId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.DzialId), "Podany dzial nie istnieje.");
            return ValidationProblem(ModelState);
        }

        if (!await PositionExists(request.StanowiskoId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.StanowiskoId), "Podane stanowisko nie istnieje.");
            return ValidationProblem(ModelState);
        }

        employee.Imie = request.Imie.Trim();
        employee.Nazwisko = request.Nazwisko.Trim();
        employee.Login = normalizedLogin;
        employee.Rola = request.Rola;
        employee.CzyAktywny = request.CzyAktywny;
        employee.DzialId = request.DzialId;
        employee.StanowiskoId = request.StanowiskoId;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            employee.HasloHash = passwordHasher.HashPassword(employee, request.Password);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetEmployeeResponse(employee.Id, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Pracownicy
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (employee is null)
        {
            return NotFoundProblem("Pracownik o podanym identyfikatorze nie istnieje.");
        }

        employee.CzyAktywny = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool ValidateEmployeeInput(
        string? imie,
        string imieField,
        string? nazwisko,
        string nazwiskoField,
        string? login,
        string loginField,
        EmployeeRole rola,
        long dzialId,
        long stanowiskoId)
    {
        ValidateRequiredText(imie, imieField, "Imie", 100);
        ValidateRequiredText(nazwisko, nazwiskoField, "Nazwisko", 100);
        ValidateRequiredText(login, loginField, "Login", 100);

        if (!Enum.IsDefined(rola))
        {
            ModelState.AddModelError(nameof(CreateEmployeeRequest.Rola), "Rola ma niepoprawna wartosc.");
        }

        if (dzialId <= 0)
        {
            ModelState.AddModelError(nameof(CreateEmployeeRequest.DzialId), "Identyfikator dzialu musi byc wiekszy od zera.");
        }

        if (stanowiskoId <= 0)
        {
            ModelState.AddModelError(nameof(CreateEmployeeRequest.StanowiskoId), "Identyfikator stanowiska musi byc wiekszy od zera.");
        }

        return ModelState.IsValid;
    }

    private void ValidateRequiredText(string? value, string field, string label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ModelState.AddModelError(field, $"{label} jest wymagane.");
        }
        else if (value.Trim().Length > maxLength)
        {
            ModelState.AddModelError(field, $"{label} moze miec maksymalnie {maxLength} znakow.");
        }
    }

    private async Task<bool> DepartmentExists(long id, CancellationToken cancellationToken)
    {
        return await dbContext.Dzialy.AnyAsync(d => d.Id == id, cancellationToken);
    }

    private async Task<bool> PositionExists(long id, CancellationToken cancellationToken)
    {
        return await dbContext.Stanowiska.AnyAsync(s => s.Id == id, cancellationToken);
    }

    private async Task<EmployeeResponse?> GetEmployeeResponse(long id, CancellationToken cancellationToken)
    {
        return await dbContext.Pracownicy
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                Imie = e.Imie,
                Nazwisko = e.Nazwisko,
                Login = e.Login,
                Rola = e.Rola.ToString(),
                CzyAktywny = e.CzyAktywny,
                DzialId = e.DzialId,
                Dzial = e.Dzial.Nazwa,
                FirmaId = e.Dzial.FirmaId,
                Firma = e.Dzial.Firma.Nazwa,
                StanowiskoId = e.StanowiskoId,
                Stanowisko = e.Stanowisko.Nazwa
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
