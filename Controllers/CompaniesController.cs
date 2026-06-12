using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Contracts.Companies;
using WebAPI.Data;
using WebAPI.Domain.Entities;

namespace WebAPI.Controllers;

[ApiController]
[Route("companies")]
[Authorize(Roles = nameof(EmployeeRole.Administrator))]
public class CompaniesController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<CompanyResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CompanyResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var companies = await dbContext.Firmy
            .AsNoTracking()
            .OrderBy(f => f.Nazwa)
            .Select(f => new CompanyResponse
            {
                Id = f.Id,
                Nazwa = f.Nazwa,
                Nip = f.Nip
            })
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var company = await dbContext.Firmy
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new CompanyResponse
            {
                Id = f.Id,
                Nazwa = f.Nazwa,
                Nip = f.Nip
            })
            .SingleOrDefaultAsync(cancellationToken);

        return company is null
            ? NotFoundProblem("Firma o podanym identyfikatorze nie istnieje.")
            : Ok(company);
    }

    [HttpPost]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompanyResponse>> Create(
        CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateCompanyInput(request.Nazwa, nameof(request.Nazwa), request.Nip, nameof(request.Nip)))
        {
            return ValidationProblem(ModelState);
        }

        var normalizedNip = request.Nip.Trim();
        var nipExists = await dbContext.Firmy
            .AnyAsync(f => f.Nip == normalizedNip, cancellationToken);

        if (nipExists)
        {
            return ConflictProblem("Firma o podanym NIP juz istnieje.");
        }

        var company = new Firma
        {
            Nazwa = request.Nazwa.Trim(),
            Nip = normalizedNip
        };

        dbContext.Firmy.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/companies/{company.Id}", MapCompanyResponse(company));
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompanyResponse>> Update(
        long id,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateCompanyInput(request.Nazwa, nameof(request.Nazwa), request.Nip, nameof(request.Nip)))
        {
            return ValidationProblem(ModelState);
        }

        var company = await dbContext.Firmy
            .SingleOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (company is null)
        {
            return NotFoundProblem("Firma o podanym identyfikatorze nie istnieje.");
        }

        var normalizedNip = request.Nip.Trim();
        var nipExists = await dbContext.Firmy
            .AnyAsync(f => f.Id != id && f.Nip == normalizedNip, cancellationToken);

        if (nipExists)
        {
            return ConflictProblem("Firma o podanym NIP juz istnieje.");
        }

        company.Nazwa = request.Nazwa.Trim();
        company.Nip = normalizedNip;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapCompanyResponse(company));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var company = await dbContext.Firmy
            .SingleOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (company is null)
        {
            return NotFoundProblem("Firma o podanym identyfikatorze nie istnieje.");
        }

        var hasDepartments = await dbContext.Dzialy
            .AnyAsync(d => d.FirmaId == id, cancellationToken);

        if (hasDepartments)
        {
            return ConflictProblem("Nie mozna usunac firmy, do ktorej sa przypisane dzialy.");
        }

        dbContext.Firmy.Remove(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool ValidateCompanyInput(string nazwa, string nazwaField, string nip, string nipField)
    {
        if (string.IsNullOrWhiteSpace(nazwa))
        {
            ModelState.AddModelError(nazwaField, "Nazwa jest wymagana.");
        }
        else if (nazwa.Trim().Length > 200)
        {
            ModelState.AddModelError(nazwaField, "Nazwa moze miec maksymalnie 200 znakow.");
        }

        if (string.IsNullOrWhiteSpace(nip))
        {
            ModelState.AddModelError(nipField, "NIP jest wymagany.");
        }
        else
        {
            var normalizedNip = nip.Trim();
            if (normalizedNip.Length != 10 || !normalizedNip.All(char.IsDigit))
            {
                ModelState.AddModelError(nipField, "NIP musi skladac sie z 10 cyfr.");
            }
        }

        return ModelState.IsValid;
    }

    private static CompanyResponse MapCompanyResponse(Firma company)
    {
        return new CompanyResponse
        {
            Id = company.Id,
            Nazwa = company.Nazwa,
            Nip = company.Nip
        };
    }
}
