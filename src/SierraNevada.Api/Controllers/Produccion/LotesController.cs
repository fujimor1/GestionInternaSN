using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Produccion.Calibracion;
using SierraNevada.Application.Produccion.CasosDeUso;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Api.Controllers.Produccion;

public sealed record RegistrarMuestreoRequest(
    DateOnly Fecha, decimal PesoPromedioMuestreadoGr, decimal TallaPromedioMuestreadaCm, int NumeroPecesMuestreados);

public sealed record RegistrarAlimentacionRequest(DateOnly Fecha, decimal CantidadKgEntregada, TipoAlimento TipoAlimento);

public sealed record RegistrarMortalidadRequest(DateOnly Fecha, int Cantidad);

public sealed record RegistrarCondicionesRequest(
    DateOnly Fecha, decimal? TempAguaC, decimal? Ph, decimal? OxigenoMgL, decimal? AmoniacoMgL);

public sealed record RealizarSeleccionRequest(
    int CantidadAMover, int NuevaUnidadProduccionId, decimal PesoPromedioGr, decimal TallaPromedioCm, DateOnly Fecha);

public sealed record CambiarEtapaRequest(EtapaProductiva NuevaEtapa, DateOnly Fecha);

/// <summary>
/// Lotes y toda la captura real asociada (muestreos, alimentación, mortalidad, condiciones de
/// agua, selección, cambio de etapa) + el cálculo de calibración. Las escrituras que solo
/// insertan un registro sin tocar otras entidades (condiciones) van directo al repositorio;
/// las que orquestan varias entidades (muestreo, mortalidad, selección, cambio de etapa) pasan
/// por los casos de uso de Application.
/// </summary>
[ApiController]
[Route("api/produccion/lotes")]
[Authorize]
public sealed class LotesController(
    ILoteRepository loteRepository,
    IMuestreoRepository muestreoRepository,
    IRegistroCondicionesRepository condicionesRepository,
    RegistrarMuestreoService registrarMuestreoService,
    RegistrarAlimentacionRealService registrarAlimentacionService,
    RegistrarMortalidadService registrarMortalidadService,
    RealizarSeleccionService realizarSeleccionService,
    CambiarEtapaService cambiarEtapaService,
    CalcularCalibracionLoteService calcularCalibracionService) : ApiControllerBase
{
    private const string RolesCaptura = $"{nameof(RolUsuario.Administrador)},{nameof(RolUsuario.Operario)}";

    [HttpGet]
    public async Task<IActionResult> ListarActivos(CancellationToken ct)
        => Ok(await loteRepository.ListarActivosAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct)
    {
        var lote = await loteRepository.ObtenerPorIdAsync(id, ct);
        return lote is null ? NotFound() : Ok(lote);
    }

    [HttpGet("por-campania/{campañaId:int}")]
    public async Task<IActionResult> ListarPorCampaña(int campañaId, CancellationToken ct)
        => Ok(await loteRepository.ListarPorCampañaAsync(campañaId, ct));

    [HttpGet("{id:int}/muestreos")]
    public async Task<IActionResult> ListarMuestreos(int id, CancellationToken ct)
        => Ok(await muestreoRepository.ListarPorLoteAsync(id, ct));

    [HttpPost("{id:int}/muestreos")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> RegistrarMuestreo(int id, RegistrarMuestreoRequest request, CancellationToken ct)
    {
        var registradoPor = User.Identity?.Name ?? "desconocido";
        var resultado = await registrarMuestreoService.EjecutarAsync(
            id, request.Fecha, request.PesoPromedioMuestreadoGr, request.TallaPromedioMuestreadaCm,
            request.NumeroPecesMuestreados, registradoPor, ct);

        return resultado.Match(muestreoId => Ok(new { id = muestreoId }), ProblemFromErrors);
    }

    [HttpPost("{id:int}/alimentacion")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> RegistrarAlimentacion(int id, RegistrarAlimentacionRequest request, CancellationToken ct)
    {
        var resultado = await registrarAlimentacionService.EjecutarAsync(
            id, request.Fecha, request.CantidadKgEntregada, request.TipoAlimento, ct);

        return resultado.Match(registroId => Ok(new { id = registroId }), ProblemFromErrors);
    }

    [HttpPost("{id:int}/mortalidad")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> RegistrarMortalidad(int id, RegistrarMortalidadRequest request, CancellationToken ct)
    {
        var registradoPor = User.Identity?.Name ?? "desconocido";
        var resultado = await registrarMortalidadService.EjecutarAsync(id, request.Fecha, request.Cantidad, registradoPor, ct);

        return resultado.Match(registroId => Ok(new { id = registroId }), ProblemFromErrors);
    }

    [HttpPost("{id:int}/condiciones")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> RegistrarCondiciones(int id, RegistrarCondicionesRequest request, CancellationToken ct)
    {
        var resultado = RegistroCondiciones.Crear(
            id, request.Fecha, request.TempAguaC, request.Ph, request.OxigenoMgL, request.AmoniacoMgL);

        if (resultado.IsError)
            return ProblemFromErrors(resultado.Errors);

        var registroId = await condicionesRepository.CrearAsync(resultado.Value, ct);
        return Ok(new { id = registroId });
    }

    [HttpPost("{id:int}/seleccion")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> RealizarSeleccion(int id, RealizarSeleccionRequest request, CancellationToken ct)
    {
        var resultado = await realizarSeleccionService.EjecutarAsync(
            id, request.CantidadAMover, request.NuevaUnidadProduccionId,
            request.PesoPromedioGr, request.TallaPromedioCm, request.Fecha, ct);

        return resultado.Match(
            loteHijoId => CreatedAtAction(nameof(ObtenerPorId), new { id = loteHijoId }, new { id = loteHijoId }),
            ProblemFromErrors);
    }

    [HttpPost("{id:int}/cambio-etapa")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> CambiarEtapa(int id, CambiarEtapaRequest request, CancellationToken ct)
    {
        var resultado = await cambiarEtapaService.EjecutarAsync(id, request.NuevaEtapa, request.Fecha, ct);
        return resultado.Match(_ => NoContent(), ProblemFromErrors);
    }

    [HttpGet("{id:int}/calibracion")]
    public async Task<IActionResult> ObtenerCalibracion(int id, CancellationToken ct)
    {
        var resultado = await calcularCalibracionService.CalcularAsync(id, ct);
        return resultado.Match(Ok, ProblemFromErrors);
    }
}
