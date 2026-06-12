using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Contracts.Positions;
using WebAPI.Data;
using WebAPI.Domain.Entities;

namespace WebAPI.Controllers;

[ApiController]
[Route("positions")]
[Authorize(Roles = nameof(EmployeeRole.Administrator))]
public class PositionsController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PositionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PositionResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var positions = await dbContext.Stanowiska
            .AsNoTracking()
            .OrderBy(s => s.Nazwa)
            .Select(s => new PositionResponse
            {
                Id = s.Id,
                Nazwa = s.Nazwa
            })
            .ToListAsync(cancellationToken);

        return Ok(positions);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<PositionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var position = await dbContext.Stanowiska
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new PositionResponse
            {
                Id = s.Id,
                Nazwa = s.Nazwa
            })
            .SingleOrDefaultAsync(cancellationToken);

        return position is null
            ? NotFoundProblem("Stanowisko o podanym identyfikatorze nie istnieje.")
            : Ok(position);
    }

    [HttpPost]
    [ProducesResponseType<PositionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PositionResponse>> Create(
        CreatePositionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidatePositionInput(request.Nazwa, nameof(request.Nazwa)))
        {
            return ValidationProblem(ModelState);
        }

        var position = new Stanowisko
        {
            Nazwa = request.Nazwa.Trim()
        };

        dbContext.Stanowiska.Add(position);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/positions/{position.Id}", MapPositionResponse(position));
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<PositionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionResponse>> Update(
        long id,
        UpdatePositionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidatePositionInput(request.Nazwa, nameof(request.Nazwa)))
        {
            return ValidationProblem(ModelState);
        }

        var position = await dbContext.Stanowiska
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (position is null)
        {
            return NotFoundProblem("Stanowisko o podanym identyfikatorze nie istnieje.");
        }

        position.Nazwa = request.Nazwa.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapPositionResponse(position));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var position = await dbContext.Stanowiska
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (position is null)
        {
            return NotFoundProblem("Stanowisko o podanym identyfikatorze nie istnieje.");
        }

        var hasEmployees = await dbContext.Pracownicy
            .AnyAsync(p => p.StanowiskoId == id, cancellationToken);

        if (hasEmployees)
        {
            return ConflictProblem("Nie mozna usunac stanowiska, do ktorego sa przypisani pracownicy.");
        }

        dbContext.Stanowiska.Remove(position);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool ValidatePositionInput(string nazwa, string nazwaField)
    {
        if (string.IsNullOrWhiteSpace(nazwa))
        {
            ModelState.AddModelError(nazwaField, "Nazwa jest wymagana.");
        }
        else if (nazwa.Trim().Length > 200)
        {
            ModelState.AddModelError(nazwaField, "Nazwa moze miec maksymalnie 200 znakow.");
        }

        return ModelState.IsValid;
    }

    private static PositionResponse MapPositionResponse(Stanowisko position)
    {
        return new PositionResponse
        {
            Id = position.Id,
            Nazwa = position.Nazwa
        };
    }
}
