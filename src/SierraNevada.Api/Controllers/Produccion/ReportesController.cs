using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Produccion.Reportes;

namespace SierraNevada.Api.Controllers.Produccion;

[ApiController]
[Route("api/produccion/reportes")]
[Authorize]
public sealed class ReportesController(CalcularReporteProduccionGlobalService reporteService) : ApiControllerBase
{
    [HttpGet("global")]
    public async Task<IActionResult> ObtenerReporteGlobal(CancellationToken ct)
        => Ok(await reporteService.CalcularAsync(ct));
}
