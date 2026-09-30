using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Inventario.CasosDeUso;

namespace SierraNevada.Api.Controllers.Inventario;

[ApiController]
[Route("api/inventario/reorden")]
public sealed class ReordenController(CalcularPlanReordenService reordenService) : ApiControllerBase
{
    /// <summary>
    /// Calcula los parámetros de optimización de inventario: Punto de Reorden (ROP),
    /// Stock de Seguridad (SS) y Cantidad Económica de Pedido (EOQ) por cada tipo de alimento.
    /// </summary>
    [HttpGet("plan")]
    public async Task<IActionResult> ObtenerPlanReorden(
        [FromQuery] double nivelServicioZ = 1.65,
        [FromQuery] int diasHistoricoDemanda = 60,
        CancellationToken ct = default)
    {
        var plan = await reordenService.EjecutarAsync(nivelServicioZ, diasHistoricoDemanda, ct);
        return Ok(plan);
    }
}
