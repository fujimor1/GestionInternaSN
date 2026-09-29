using ErrorOr;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.CasosDeUso;

/// <summary>
/// Registra un muestreo real (protocolo FONDEPES "Inventario") y actualiza el estado
/// "actual" cacheado del lote. Es la pieza que resuelve el gap #1 (docs/diseno-modulo-produccion.md):
/// preserva la serie de tiempo de peso/talla en vez de sobrescribirla sin historial.
/// </summary>
public sealed class RegistrarMuestreoService(
    ILoteRepository loteRepository,
    IMuestreoRepository muestreoRepository,
    IHistorialMovimientoRepository historialRepository)
{
    public async Task<ErrorOr<int>> EjecutarAsync(
        int loteId,
        DateOnly fecha,
        decimal pesoPromedioMuestreadoGr,
        decimal tallaPromedioMuestreadaCm,
        int numeroPecesMuestreados,
        string registradoPor,
        CancellationToken ct = default)
    {
        var lote = await loteRepository.ObtenerPorIdAsync(loteId, ct);
        if (lote is null)
            return Error.NotFound("Lote.NoEncontrado", "El lote no existe.");

        // La cantidad de peces vivos NO la reporta el operario en el muestreo (solo pesa/mide una
        // muestra) — se toma del conteo actual del lote, que ya es correcto vía mortalidad/selección.
        var muestreoResult = Muestreo.Crear(
            loteId, fecha, pesoPromedioMuestreadoGr, tallaPromedioMuestreadaCm,
            numeroPecesMuestreados, lote.CantidadTotalPeces, registradoPor);
        if (muestreoResult.IsError)
            return muestreoResult.Errors;

        var muestreo = muestreoResult.Value;

        var actualizarResult = lote.ActualizarConMuestreo(muestreo);
        if (actualizarResult.IsError)
            return actualizarResult.Errors;

        // NOTA: estas 3 operaciones no están en una sola transacción de base de datos todavía
        // (cada repositorio ADO.NET abre su propia conexión) — ver limitación documentada en
        // docs/arquitectura-tecnica.md sección 3 (pendiente: Unit of Work).
        var muestreoId = await muestreoRepository.CrearAsync(muestreo, ct);
        await loteRepository.ActualizarAsync(lote, ct);

        var historialResult = HistorialMovimiento.Crear(
            loteId,
            fecha.ToDateTime(TimeOnly.MinValue),
            TipoMovimientoHistorial.Medicion,
            $"Muestreo registrado: {numeroPecesMuestreados} peces medidos, peso promedio {pesoPromedioMuestreadoGr}g, talla {tallaPromedioMuestreadaCm}cm.");

        if (!historialResult.IsError)
            await historialRepository.CrearAsync(historialResult.Value, ct);

        return muestreoId;
    }
}
