using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Contracts.Permissions;
using WebAPI.Data;
using WebAPI.Domain.Entities;

namespace WebAPI.Controllers;

[ApiController]
[Route("permissions")]
[Authorize(Roles = nameof(EmployeeRole.Administrator))]
public class PermissionsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PermissionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionResponse>>> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Uprawnienia
            .AsNoTracking()
            .Include(u => u.PracownikZarzadzajacy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(u =>
                EF.Functions.ILike(u.Sygnatura, $"%{normalizedSearch}%") ||
                EF.Functions.ILike(u.Nazwa, $"%{normalizedSearch}%") ||
                EF.Functions.ILike(u.Opis, $"%{normalizedSearch}%"));
        }

        var permissions = await query
            .OrderBy(u => u.Nazwa)
            .Select(u => new PermissionResponse
            {
                Sygnatura = u.Sygnatura,
                Nazwa = u.Nazwa,
                Opis = u.Opis,
                PracownikZarzadzajacyId = u.PracownikZarzadzajacyId,
                PracownikZarzadzajacy = $"{u.PracownikZarzadzajacy.Imie} {u.PracownikZarzadzajacy.Nazwisko}"
            })
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }

    [HttpGet("{sygnatura}")]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionResponse>> GetBySignature(string sygnatura, CancellationToken cancellationToken)
    {
        var permission = await dbContext.Uprawnienia
            .AsNoTracking()
            .Include(u => u.PracownikZarzadzajacy)
            .Where(u => u.Sygnatura == sygnatura)
            .Select(u => new PermissionResponse
            {
                Sygnatura = u.Sygnatura,
                Nazwa = u.Nazwa,
                Opis = u.Opis,
                PracownikZarzadzajacyId = u.PracownikZarzadzajacyId,
                PracownikZarzadzajacy = $"{u.PracownikZarzadzajacy.Imie} {u.PracownikZarzadzajacy.Nazwisko}"
            })
            .SingleOrDefaultAsync(cancellationToken);

        return permission is null
            ? NotFound(new { message = "Uprawnienie o podanej sygnaturze nie istnieje." })
            : Ok(permission);
    }

    [HttpPost]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermissionResponse>> Create(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        if (!ValidatePermissionInput(
                request.Sygnatura,
                nameof(request.Sygnatura),
                request.Nazwa,
                nameof(request.Nazwa),
                request.Opis,
                nameof(request.Opis)))
        {
            return ValidationProblem(ModelState);
        }

        var permissionExists = await dbContext.Uprawnienia
            .AnyAsync(u => u.Sygnatura == request.Sygnatura, cancellationToken);

        if (permissionExists)
        {
            return Conflict(new { message = "Uprawnienie o podanej sygnaturze juz istnieje." });
        }

        var manager = await dbContext.Pracownicy
            .SingleOrDefaultAsync(p => p.Id == request.PracownikZarzadzajacyId, cancellationToken);

        if (manager is null)
        {
            ModelState.AddModelError(
                nameof(request.PracownikZarzadzajacyId),
                "Podany pracownik zarzadzajacy nie istnieje.");
            return ValidationProblem(ModelState);
        }

        var permission = new Uprawnienie
        {
            Sygnatura = request.Sygnatura.Trim(),
            Nazwa = request.Nazwa.Trim(),
            Opis = request.Opis.Trim(),
            PracownikZarzadzajacyId = request.PracownikZarzadzajacyId
        };

        dbContext.Uprawnienia.Add(permission);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created(
            $"/permissions/{permission.Sygnatura}",
            MapPermissionResponse(permission, manager));
    }

    [HttpPut("{sygnatura}")]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionResponse>> Update(
        string sygnatura,
        UpdatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidatePermissionInput(
                sygnatura,
                "sygnatura",
                request.Nazwa,
                nameof(request.Nazwa),
                request.Opis,
                nameof(request.Opis)))
        {
            return ValidationProblem(ModelState);
        }

        var permission = await dbContext.Uprawnienia
            .SingleOrDefaultAsync(u => u.Sygnatura == sygnatura, cancellationToken);

        if (permission is null)
        {
            return NotFound(new { message = "Uprawnienie o podanej sygnaturze nie istnieje." });
        }

        var manager = await dbContext.Pracownicy
            .SingleOrDefaultAsync(p => p.Id == request.PracownikZarzadzajacyId, cancellationToken);

        if (manager is null)
        {
            ModelState.AddModelError(
                nameof(request.PracownikZarzadzajacyId),
                "Podany pracownik zarzadzajacy nie istnieje.");
            return ValidationProblem(ModelState);
        }

        permission.Nazwa = request.Nazwa.Trim();
        permission.Opis = request.Opis.Trim();
        permission.PracownikZarzadzajacyId = request.PracownikZarzadzajacyId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapPermissionResponse(permission, manager));
    }

    [HttpDelete("{sygnatura}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string sygnatura, CancellationToken cancellationToken)
    {
        var permission = await dbContext.Uprawnienia
            .SingleOrDefaultAsync(u => u.Sygnatura == sygnatura, cancellationToken);

        if (permission is null)
        {
            return NotFound(new { message = "Uprawnienie o podanej sygnaturze nie istnieje." });
        }

        dbContext.Uprawnienia.Remove(permission);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("employees/search")]
    [ProducesResponseType<IReadOnlyCollection<PermissionEmployeeSearchResultResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionEmployeeSearchResultResponse>>> SearchEmployees(
        [FromQuery] string? number,
        [FromQuery] string? firstName,
        [FromQuery] string? lastName,
        [FromQuery] string? department,
        [FromQuery] string? company,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Pracownicy
            .AsNoTracking()
            .Include(p => p.Dzial)
            .ThenInclude(d => d.Firma)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(number))
        {
            var normalizedNumber = number.Trim();
            if (!long.TryParse(normalizedNumber, out var employeeNumber))
            {
                return Ok(Array.Empty<PermissionEmployeeSearchResultResponse>());
            }

            query = query.Where(p => p.Id == employeeNumber);
        }

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            var normalizedFirstName = firstName.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Imie, $"%{normalizedFirstName}%"));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            var normalizedLastName = lastName.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Nazwisko, $"%{normalizedLastName}%"));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var normalizedDepartment = department.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Dzial.Nazwa, $"%{normalizedDepartment}%"));
        }

        if (!string.IsNullOrWhiteSpace(company))
        {
            var normalizedCompany = company.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Dzial.Firma.Nazwa, $"%{normalizedCompany}%"));
        }

        var employees = await query
            .OrderBy(p => p.Nazwisko)
            .ThenBy(p => p.Imie)
            .Select(p => new PermissionEmployeeSearchResultResponse
            {
                Id = p.Id,
                Numer = p.Id.ToString(),
                Imie = p.Imie,
                Nazwisko = p.Nazwisko,
                Dzial = p.Dzial.Nazwa,
                Firma = p.Dzial.Firma.Nazwa
            })
            .ToListAsync(cancellationToken);

        return Ok(employees);
    }

    [HttpGet("employees/{employeeId:long}/permissions")]
    [ProducesResponseType<EmployeePermissionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeePermissionsResponse>> GetEmployeePermissions(
        long employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.Pracownicy
            .AsNoTracking()
            .Include(p => p.Dzial)
            .ThenInclude(d => d.Firma)
            .SingleOrDefaultAsync(p => p.Id == employeeId, cancellationToken);

        if (employee is null)
        {
            return NotFound(new { message = "Pracownik o podanym identyfikatorze nie istnieje." });
        }

        var assignments = await dbContext.PracownikUprawnienia
            .AsNoTracking()
            .Where(pu => pu.PracownikId == employeeId)
            .ToDictionaryAsync(pu => pu.SygnaturaUprawnienia, cancellationToken);

        var permissions = await dbContext.Uprawnienia
            .AsNoTracking()
            .OrderBy(u => u.Nazwa)
            .Select(u => new EmployeePermissionResponse
            {
                Sygnatura = u.Sygnatura,
                Nazwa = u.Nazwa,
                Opis = u.Opis,
                CzyPrzypisane = assignments.ContainsKey(u.Sygnatura),
                DataNadania = assignments.ContainsKey(u.Sygnatura)
                    ? assignments[u.Sygnatura].DataNadania
                    : null,
                WazneDo = assignments.ContainsKey(u.Sygnatura)
                    ? assignments[u.Sygnatura].WazneDo
                    : null
            })
            .ToListAsync(cancellationToken);

        return Ok(new EmployeePermissionsResponse
        {
            Id = employee.Id,
            Numer = employee.Id.ToString(),
            Imie = employee.Imie,
            Nazwisko = employee.Nazwisko,
            Dzial = employee.Dzial.Nazwa,
            Firma = employee.Dzial.Firma.Nazwa,
            Uprawnienia = permissions
        });
    }

    [HttpPost("employees/{employeeId:long}/permissions")]
    [ProducesResponseType<EmployeePermissionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeePermissionsResponse>> AssignPermissions(
        long employeeId,
        AssignPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SygnaturyUprawnien.Count == 0)
        {
            ModelState.AddModelError(
                nameof(request.SygnaturyUprawnien),
                "Nalezy podac przynajmniej jedna sygnature uprawnienia.");
            return ValidationProblem(ModelState);
        }

        var normalizedSignatures = request.SygnaturyUprawnien
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedSignatures.Count != request.SygnaturyUprawnien.Count)
        {
            ModelState.AddModelError(
                nameof(request.SygnaturyUprawnien),
                "Lista sygnatur nie moze zawierac pustych ani zduplikowanych wartosci.");
            return ValidationProblem(ModelState);
        }

        var employeeExists = await dbContext.Pracownicy
            .AnyAsync(p => p.Id == employeeId, cancellationToken);

        if (!employeeExists)
        {
            return NotFound(new { message = "Pracownik o podanym identyfikatorze nie istnieje." });
        }

        var existingPermissions = await dbContext.Uprawnienia
            .Where(u => normalizedSignatures.Contains(u.Sygnatura))
            .Select(u => u.Sygnatura)
            .ToListAsync(cancellationToken);

        var missingPermissions = normalizedSignatures
            .Except(existingPermissions, StringComparer.Ordinal)
            .ToList();

        if (missingPermissions.Count > 0)
        {
            ModelState.AddModelError(
                nameof(request.SygnaturyUprawnien),
                $"Nie istnieja uprawnienia: {string.Join(", ", missingPermissions)}.");
            return ValidationProblem(ModelState);
        }

        var alreadyAssigned = await dbContext.PracownikUprawnienia
            .Where(pu => pu.PracownikId == employeeId && normalizedSignatures.Contains(pu.SygnaturaUprawnienia))
            .Select(pu => pu.SygnaturaUprawnienia)
            .ToListAsync(cancellationToken);

        var signaturesToAssign = normalizedSignatures
            .Except(alreadyAssigned, StringComparer.Ordinal)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var signature in signaturesToAssign)
        {
            dbContext.PracownikUprawnienia.Add(new PracownikUprawnienie
            {
                PracownikId = employeeId,
                SygnaturaUprawnienia = signature,
                DataNadania = today
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetEmployeePermissions(employeeId, cancellationToken);
    }

    [HttpDelete("employees/{employeeId:long}/permissions/{sygnatura}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermissionFromEmployee(
        long employeeId,
        string sygnatura,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.PracownikUprawnienia
            .SingleOrDefaultAsync(
                pu => pu.PracownikId == employeeId && pu.SygnaturaUprawnienia == sygnatura,
                cancellationToken);

        if (assignment is null)
        {
            return NotFound(new { message = "Przypisanie uprawnienia do pracownika nie istnieje." });
        }

        dbContext.PracownikUprawnienia.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool ValidatePermissionInput(
        string sygnatura,
        string sygnaturaField,
        string nazwa,
        string nazwaField,
        string opis,
        string opisField)
    {
        if (string.IsNullOrWhiteSpace(sygnatura))
        {
            ModelState.AddModelError(sygnaturaField, "Sygnatura jest wymagana.");
        }

        if (string.IsNullOrWhiteSpace(nazwa))
        {
            ModelState.AddModelError(nazwaField, "Nazwa jest wymagana.");
        }

        if (string.IsNullOrWhiteSpace(opis))
        {
            ModelState.AddModelError(opisField, "Opis jest wymagany.");
        }

        return ModelState.IsValid;
    }

    private static PermissionResponse MapPermissionResponse(Uprawnienie permission, Pracownik manager)
    {
        return new PermissionResponse
        {
            Sygnatura = permission.Sygnatura,
            Nazwa = permission.Nazwa,
            Opis = permission.Opis,
            PracownikZarzadzajacyId = permission.PracownikZarzadzajacyId,
            PracownikZarzadzajacy = $"{manager.Imie} {manager.Nazwisko}"
        };
    }
}
