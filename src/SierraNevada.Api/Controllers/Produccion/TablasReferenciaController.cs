using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Api.Controllers.Produccion;

public sealed record ValorReferenciaRequest(
    decimal TallaMinCm, decimal TallaMaxCm, decimal? TemperaturaMinC, decimal? TemperaturaMaxC, decimal Valor);

public sealed record CrearTablaReferenciaRequest(
    TipoTablaReferencia Tipo, MarcoReferencia Marco, DateOnly FechaVigenciaDesde, string Fuente,
    IReadOnlyList<ValorReferenciaRequest> Valores);

/// <summary>
/// Tablas de ración/FCA/mortalidad/densidad — reemplazan lo que en el sistema anterior estaba
/// hardcodeado en Python (docs/diseno-modulo-produccion.md gap #3). Cada creación es una nueva
/// versión activada, sin borrar el historial — así se puede auditar cómo evolucionó la
/// calibración del sistema con el tiempo.
/// </summary>
[ApiController]
[Route("api/produccion/tablas-referencia")]
[Authorize]
public sealed class TablasReferenciaController(ITablaReferenciaVersionRepository tablaReferenciaRepository) : ApiControllerBase
{
    [HttpGet("{tipo}/{marco}/activa")]
    public async Task<IActionResult> ObtenerActiva(TipoTablaReferencia tipo, MarcoReferencia marco, CancellationToken ct)
    {
        var version = await tablaReferenciaRepository.ObtenerActivaAsync(tipo, marco, ct);
        return version is null ? NotFound() : Ok(version);
    }

    [HttpGet("{tipo}/{marco}")]
    public async Task<IActionResult> ListarHistorial(TipoTablaReferencia tipo, MarcoReferencia marco, CancellationToken ct)
        => Ok(await tablaReferenciaRepository.ListarPorTipoAsync(tipo, marco, ct));

    /// <summary>
    /// Crea y activa una nueva versión (desactivando la anterior del mismo tipo). Solo Administrador:
    /// esto cambia el estándar que usa todo el sistema para calcular ración/FCA/mortalidad/densidad.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    public async Task<IActionResult> CrearYActivar(CrearTablaReferenciaRequest request, CancellationToken ct)
    {
        var versionResult = TablaReferenciaVersion.Crear(request.Tipo, request.Marco, request.FechaVigenciaDesde, request.Fuente);
        if (versionResult.IsError)
            return ProblemFromErrors(versionResult.Errors);

        var version = versionResult.Value;
        foreach (var valor in request.Valores)
        {
            var agregarResult = version.AgregarValor(valor.TallaMinCm, valor.TallaMaxCm, valor.Valor, valor.TemperaturaMinC, valor.TemperaturaMaxC);
            if (agregarResult.IsError)
                return ProblemFromErrors(agregarResult.Errors);
        }

        var id = await tablaReferenciaRepository.CrearYActivarAsync(version, ct);
        return CreatedAtAction(nameof(ObtenerActiva), new { tipo = request.Tipo, marco = request.Marco }, new { id });
    }
}
