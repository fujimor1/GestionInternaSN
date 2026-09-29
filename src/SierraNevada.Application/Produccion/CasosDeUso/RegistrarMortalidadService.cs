using ErrorOr;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.CasosDeUso;

/// <summary>Registra bajas reales y descuenta del conteo vivo del lote.</summary>
public sealed class RegistrarMortalidadService(
    ILoteRepository loteRepository,
    IRegistroMortalidadRepository mortalidadRepository,
    IHistorialMovimientoRepository historialRepository)
{
    public async Task<ErrorOr<int>> EjecutarAsync(
        int loteId, DateOnly fecha, int cantidad, string registradoPor, CancellationToken ct = default)
    {
        var lote = await loteRepository.ObtenerPorIdAsync(loteId, ct);
        if (lote is null)
            return Error.NotFound("Lote.NoEncontrado", "El lote no existe.");

        var registroResult = RegistroMortalidad.Crear(loteId, fecha, cantidad, registradoPor);
        if (registroResult.IsError)
            return registroResult.Errors;

        var descontarResult = lote.RegistrarMortalidad(cantidad);
        if (descontarResult.IsError)
            return descontarResult.Errors;

        var registroId = await mortalidadRepository.CrearAsync(registroResult.Value, ct);
        await loteRepository.ActualizarAsync(lote, ct);

        var historialResult = HistorialMovimiento.Crear(
            loteId, fecha.ToDateTime(TimeOnly.MinValue), TipoMovimientoHistorial.Bajas,
            $"{cantidad} bajas registradas.", cantidadAfectada: cantidad);

        if (!historialResult.IsError)
            await historialRepository.CrearAsync(historialResult.Value, ct);

        return registroId;
    }
}
