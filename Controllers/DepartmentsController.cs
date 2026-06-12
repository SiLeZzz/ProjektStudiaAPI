using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Contracts.Departments;
using WebAPI.Data;
using WebAPI.Domain.Entities;

namespace WebAPI.Controllers;

[ApiController]
[Route("departments")]
[Authorize(Roles = nameof(EmployeeRole.Administrator))]
public class DepartmentsController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<DepartmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DepartmentResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var departments = await dbContext.Dzialy
            .AsNoTracking()
            .Include(d => d.Firma)
            .OrderBy(d => d.Firma.Nazwa)
            .ThenBy(d => d.Nazwa)
            .Select(d => new DepartmentResponse
            {
                Id = d.Id,
                Nazwa = d.Nazwa,
                FirmaId = d.FirmaId,
                FirmaNazwa = d.Firma.Nazwa
            })
            .ToListAsync(cancellationToken);

        return Ok(departments);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var department = await dbContext.Dzialy
            .AsNoTracking()
            .Include(d => d.Firma)
            .Where(d => d.Id == id)
            .Select(d => new DepartmentResponse
            {
                Id = d.Id,
                Nazwa = d.Nazwa,
                FirmaId = d.FirmaId,
                FirmaNazwa = d.Firma.Nazwa
            })
            .SingleOrDefaultAsync(cancellationToken);

        return department is null
            ? NotFoundProblem("Dzial o podanym identyfikatorze nie istnieje.")
            : Ok(department);
    }

    [HttpPost]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DepartmentResponse>> Create(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateDepartmentInput(request.Nazwa, nameof(request.Nazwa), request.FirmaId, nameof(request.FirmaId)))
        {
            return ValidationProblem(ModelState);
        }

        var companyExists = await dbContext.Firmy
            .AnyAsync(f => f.Id == request.FirmaId, cancellationToken);

        if (!companyExists)
        {
            ModelState.AddModelError(nameof(request.FirmaId), "Podana firma nie istnieje.");
            return ValidationProblem(ModelState);
        }

        var department = new Dzial
        {
            Nazwa = request.Nazwa.Trim(),
            FirmaId = request.FirmaId
        };

        dbContext.Dzialy.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetDepartmentResponse(department.Id, cancellationToken);

        return Created($"/departments/{department.Id}", response);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentResponse>> Update(
        long id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateDepartmentInput(request.Nazwa, nameof(request.Nazwa), request.FirmaId, nameof(request.FirmaId)))
        {
            return ValidationProblem(ModelState);
        }

        var department = await dbContext.Dzialy
            .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department is null)
        {
            return NotFoundProblem("Dzial o podanym identyfikatorze nie istnieje.");
        }

        var companyExists = await dbContext.Firmy
            .AnyAsync(f => f.Id == request.FirmaId, cancellationToken);

        if (!companyExists)
        {
            ModelState.AddModelError(nameof(request.FirmaId), "Podana firma nie istnieje.");
            return ValidationProblem(ModelState);
        }

        department.Nazwa = request.Nazwa.Trim();
        department.FirmaId = request.FirmaId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await GetDepartmentResponse(department.Id, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var department = await dbContext.Dzialy
            .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department is null)
        {
            return NotFoundProblem("Dzial o podanym identyfikatorze nie istnieje.");
        }

        var hasEmployees = await dbContext.Pracownicy
            .AnyAsync(p => p.DzialId == id, cancellationToken);

        if (hasEmployees)
        {
            return ConflictProblem("Nie mozna usunac dzialu, do ktorego sa przypisani pracownicy.");
        }

        dbContext.Dzialy.Remove(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool ValidateDepartmentInput(string nazwa, string nazwaField, long firmaId, string firmaIdField)
    {
        if (string.IsNullOrWhiteSpace(nazwa))
        {
            ModelState.AddModelError(nazwaField, "Nazwa jest wymagana.");
        }
        else if (nazwa.Trim().Length > 200)
        {
            ModelState.AddModelError(nazwaField, "Nazwa moze miec maksymalnie 200 znakow.");
        }

        if (firmaId <= 0)
        {
            ModelState.AddModelError(firmaIdField, "Identyfikator firmy musi byc wiekszy od zera.");
        }

        return ModelState.IsValid;
    }

    private async Task<DepartmentResponse> GetDepartmentResponse(long id, CancellationToken cancellationToken)
    {
        return await dbContext.Dzialy
            .AsNoTracking()
            .Include(d => d.Firma)
            .Where(d => d.Id == id)
            .Select(d => new DepartmentResponse
            {
                Id = d.Id,
                Nazwa = d.Nazwa,
                FirmaId = d.FirmaId,
                FirmaNazwa = d.Firma.Nazwa
            })
            .SingleAsync(cancellationToken);
    }
}
