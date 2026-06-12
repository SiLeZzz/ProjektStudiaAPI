using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult NotFoundProblem(string detail)
    {
        return Problem(
            title: "Nie znaleziono zasobu.",
            detail: detail,
            statusCode: StatusCodes.Status404NotFound);
    }

    protected ObjectResult ConflictProblem(string detail)
    {
        return Problem(
            title: "Konflikt danych.",
            detail: detail,
            statusCode: StatusCodes.Status409Conflict);
    }

    protected ObjectResult UnauthorizedProblem(string detail)
    {
        return Problem(
            title: "Brak autoryzacji.",
            detail: detail,
            statusCode: StatusCodes.Status401Unauthorized);
    }

    protected ObjectResult ForbiddenProblem(string detail)
    {
        return Problem(
            title: "Brak uprawnien.",
            detail: detail,
            statusCode: StatusCodes.Status403Forbidden);
    }
}
