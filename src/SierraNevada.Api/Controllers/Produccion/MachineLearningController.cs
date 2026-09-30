using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Produccion.CasosDeUso;

namespace SierraNevada.Api.Controllers.Produccion;

[ApiController]
[Route("api/ml")]
public sealed class MachineLearningController(CalcularProyeccionTgcBayesianoService mlService) : ApiControllerBase
{
    /// <summary>
    /// Retorna las proyecciones adaptativas con modelo Grados-Día (TGC) y los parámetros
    /// actualizados con distribuciones conjugadas Bayesianas (Normal-Normal y Beta-Binomial).
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> ObtenerDashboardMl([FromQuery] int diasProyeccion = 30, CancellationToken ct = default)
    {
        var respuesta = await mlService.EjecutarAsync(diasProyeccion, ct);
        return Ok(respuesta);
    }
}
