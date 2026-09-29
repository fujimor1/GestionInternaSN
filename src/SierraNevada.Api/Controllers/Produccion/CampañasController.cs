using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Produccion.CasosDeUso;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Api.Controllers.Produccion;

public sealed record CrearCampañaRequest(
    string CodigoCampaña, DateOnly FechaSiembra, decimal PesoPromedioInicialGr, decimal TallaPromedioInicialCm,
    string? Proveedor, string? Observaciones, IReadOnlyList<DistribucionInicial> Distribuciones);

/// <summary>Siembras — pueden repartirse en varias unidades de producción desde el inicio (ver docs/diseno-modulo-produccion.md sección 5).</summary>
[ApiController]
[Route("api/produccion/campanias")]
[Authorize]
public sealed class CampañasController(
    ICampañaRepository campañaRepository, CrearCampañaConLotesService crearCampañaService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await campañaRepository.ListarTodasAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct)
    {
        var campaña = await campañaRepository.ObtenerPorIdAsync(id, ct);
        return campaña is null ? NotFound() : Ok(campaña);
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(RolUsuario.Administrador)},{nameof(RolUsuario.Operario)}")]
    public async Task<IActionResult> Crear(CrearCampañaRequest request, CancellationToken ct)
    {
        var resultado = await crearCampañaService.EjecutarAsync(
            request.CodigoCampaña, request.FechaSiembra, request.PesoPromedioInicialGr, request.TallaPromedioInicialCm,
            request.Proveedor, request.Observaciones, request.Distribuciones, ct);

        return resultado.Match(
            ok => CreatedAtAction(nameof(ObtenerPorId), new { id = ok.CampañaId }, ok),
            ProblemFromErrors);
    }
}
