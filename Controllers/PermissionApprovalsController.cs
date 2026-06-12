using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Contracts.Permissions.Approvals;
using WebAPI.Data;
using WebAPI.Domain.Entities;

namespace WebAPI.Controllers;

[ApiController]
[Route("permissions/approvals")]
[Authorize(Roles = nameof(EmployeeRole.Pracownik))]
public class PermissionApprovalsController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet("pending")]
    [ProducesResponseType<IReadOnlyCollection<PendingPermissionApprovalResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PendingPermissionApprovalResponse>>> GetPending(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentEmployeeId(out var currentEmployeeId))
        {
            return UnauthorizedProblem("Token uwierzytelniajacy jest niepoprawny.");
        }

        var approvals = await dbContext.PracownikUprawnienia
            .AsNoTracking()
            .Where(pu =>
                pu.WazneDo == null &&
                pu.Uprawnienie.PracownikZarzadzajacyId == currentEmployeeId)
            .OrderBy(pu => pu.DataNadania)
            .ThenBy(pu => pu.Pracownik.Nazwisko)
            .ThenBy(pu => pu.Pracownik.Imie)
            .Select(pu => new PendingPermissionApprovalResponse
            {
                EmployeeId = pu.PracownikId,
                EmployeeNumber = pu.PracownikId.ToString(),
                EmployeeName = $"{pu.Pracownik.Imie} {pu.Pracownik.Nazwisko}",
                Department = pu.Pracownik.Dzial.Nazwa,
                Company = pu.Pracownik.Dzial.Firma.Nazwa,
                PermissionSignature = pu.SygnaturaUprawnienia,
                PermissionName = pu.Uprawnienie.Nazwa,
                PermissionDescription = pu.Uprawnienie.Opis,
                AssignedAt = pu.DataNadania,
                ValidUntil = pu.WazneDo
            })
            .ToListAsync(cancellationToken);

        return Ok(approvals);
    }

    [HttpPost("{employeeId:long}/{signature}/approve")]
    [ProducesResponseType<PendingPermissionApprovalResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PendingPermissionApprovalResponse>> Approve(
        long employeeId,
        string signature,
        ApprovePermissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentEmployeeId(out var currentEmployeeId))
        {
            return UnauthorizedProblem("Token uwierzytelniajacy jest niepoprawny.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.ValidUntil == default)
        {
            ModelState.AddModelError(nameof(request.ValidUntil), "Data waznosci jest wymagana.");
            return ValidationProblem(ModelState);
        }

        if (request.ValidUntil < today)
        {
            ModelState.AddModelError(nameof(request.ValidUntil), "Data waznosci musi byc wieksza lub rowna dzisiejszej dacie.");
            return ValidationProblem(ModelState);
        }

        var assignment = await dbContext.PracownikUprawnienia
            .Include(pu => pu.Pracownik)
            .ThenInclude(p => p.Dzial)
            .ThenInclude(d => d.Firma)
            .Include(pu => pu.Uprawnienie)
            .SingleOrDefaultAsync(
                pu => pu.PracownikId == employeeId && pu.SygnaturaUprawnienia == signature,
                cancellationToken);

        if (assignment is null)
        {
            return NotFoundProblem("Przypisanie uprawnienia do pracownika nie istnieje.");
        }

        if (assignment.Uprawnienie.PracownikZarzadzajacyId != currentEmployeeId)
        {
            return ForbiddenProblem("Zalogowany pracownik nie zarzadza wskazanym uprawnieniem.");
        }

        if (assignment.WazneDo is not null)
        {
            return ConflictProblem("Przypisanie uprawnienia zostalo juz zatwierdzone.");
        }

        assignment.WazneDo = request.ValidUntil;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapApprovalResponse(assignment));
    }

    private bool TryGetCurrentEmployeeId(out long employeeId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdValue, out employeeId);
    }

    private static PendingPermissionApprovalResponse MapApprovalResponse(PracownikUprawnienie assignment)
    {
        return new PendingPermissionApprovalResponse
        {
            EmployeeId = assignment.PracownikId,
            EmployeeNumber = assignment.PracownikId.ToString(),
            EmployeeName = $"{assignment.Pracownik.Imie} {assignment.Pracownik.Nazwisko}",
            Department = assignment.Pracownik.Dzial.Nazwa,
            Company = assignment.Pracownik.Dzial.Firma.Nazwa,
            PermissionSignature = assignment.SygnaturaUprawnienia,
            PermissionName = assignment.Uprawnienie.Nazwa,
            PermissionDescription = assignment.Uprawnienie.Opis,
            AssignedAt = assignment.DataNadania,
            ValidUntil = assignment.WazneDo
        };
    }
}
