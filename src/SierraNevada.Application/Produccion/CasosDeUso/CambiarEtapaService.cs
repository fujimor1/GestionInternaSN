using ErrorOr;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.CasosDeUso;

/// <summary>
/// Avanza un lote a la siguiente etapa productiva y registra el evento CambioEtapa —
/// de aquí sale la duración real por etapa que pidió el usuario ("cuánto se demoró de
/// alevines 1 a alevines 2"), ver docs/diseno-modulo-produccion.md sección 4.5.
/// </summary>
public sealed class CambiarEtapaService(
    ILoteRepository loteRepository,
    IHistorialMovimientoRepository historialRepository)
{
    public async Task<ErrorOr<Success>> EjecutarAsync(
        int loteId, EtapaProductiva nuevaEtapa, DateOnly fecha, CancellationToken ct = default)
    {
        var lote = await loteRepository.ObtenerPorIdAsync(loteId, ct);
        if (lote is null)
            return Error.NotFound("Lote.NoEncontrado", "El lote no existe.");

        var etapaAnterior = lote.EtapaActual;

        var cambiarResult = lote.CambiarEtapa(nuevaEtapa, fecha);
        if (cambiarResult.IsError)
            return cambiarResult.Errors;

        await loteRepository.ActualizarAsync(lote, ct);

        var historialResult = HistorialMovimiento.Crear(
            loteId, fecha.ToDateTime(TimeOnly.MinValue), TipoMovimientoHistorial.CambioEtapa,
            $"Etapa cambiada de {etapaAnterior} a {nuevaEtapa}.",
            etapaAnterior: etapaAnterior, etapaNueva: nuevaEtapa);

        if (historialResult.IsError)
            return historialResult.Errors;

        await historialRepository.CrearAsync(historialResult.Value, ct);

        return Result.Success;
    }
}
