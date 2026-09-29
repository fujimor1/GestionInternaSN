using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace SierraNevada.Api.Controllers;

/// <summary>Mapea errores de ErrorOr a respuestas HTTP (Problem Details, RFC 9110) de forma consistente en toda la API.</summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ProblemFromErrors(List<Error> errors)
    {
        if (errors.Count == 0)
            return Problem();

        var primero = errors[0];
        return Problem(title: primero.Description, statusCode: MapearCodigoEstado(primero));
    }

    private static int MapearCodigoEstado(Error error) => error.Type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status500InternalServerError,
    };
}
