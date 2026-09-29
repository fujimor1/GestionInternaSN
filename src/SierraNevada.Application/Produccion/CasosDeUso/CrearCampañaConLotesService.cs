using ErrorOr;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.CasosDeUso;

/// <summary>
/// Una unidad y cuántos peces de la siembra le corresponden — exactamente una de
/// UnidadProduccionId (Jaula/Artesa) o BastidorId (para la etapa Ovas) debe venir informada.
/// El código de lote no se pide aquí: se genera solo (ver GeneradorCodigoLote).
/// </summary>
public sealed record DistribucionInicial(int? UnidadProduccionId, int? BastidorId, int CantidadPeces);

public sealed record CrearCampañaResultado(int CampañaId, IReadOnlyList<int> LoteIds);

/// <summary>
/// Registra una siembra (Campaña) que puede repartirse en varias unidades de producción desde
/// el inicio — el escenario que confirmó el cliente ("un lote puede ser de 90000 y esto lo
/// reparten entre varias artesas o jaulas"), ver docs/diseno-modulo-produccion.md sección 5.
/// Crea 1 Campaña + N Lote, todos enlazados por CampañaId.
/// </summary>
public sealed class CrearCampañaConLotesService(
    ICampañaRepository campañaRepository,
    ILoteRepository loteRepository,
    IUnidadProduccionRepository unidadProduccionRepository,
    IBastidorRepository bastidorRepository,
    IHistorialMovimientoRepository historialRepository)
{
    public async Task<ErrorOr<CrearCampañaResultado>> EjecutarAsync(
        string codigoCampaña,
        DateOnly fechaSiembra,
        decimal pesoPromedioInicialGr,
        decimal tallaPromedioInicialCm,
        string? proveedor,
        string? observaciones,
        IReadOnlyList<DistribucionInicial> distribuciones,
        CancellationToken ct = default)
    {
        if (distribuciones.Count == 0)
            return Error.Validation("Campaña.Distribuciones", "Debe indicarse al menos una unidad de producción para la siembra.");

        var cantidadTotal = distribuciones.Sum(d => d.CantidadPeces);

        var campañaResult = Campaña.Crear(
            codigoCampaña, fechaSiembra, cantidadTotal, pesoPromedioInicialGr, tallaPromedioInicialCm, proveedor, observaciones);
        if (campañaResult.IsError)
            return campañaResult.Errors;

        foreach (var distribucion in distribuciones)
        {
            var exactamenteUno = (distribucion.UnidadProduccionId is not null) ^ (distribucion.BastidorId is not null);
            if (!exactamenteUno)
                return Error.Validation("Campaña.Distribuciones", "Cada distribución debe indicar exactamente una unidad de producción o un bastidor, no ambos ni ninguno.");

            if (distribucion.UnidadProduccionId is int unidadId && await unidadProduccionRepository.ObtenerPorIdAsync(unidadId, ct) is null)
                return Error.NotFound("UnidadProduccion.NoEncontrada", $"La unidad {unidadId} no existe.");

            if (distribucion.BastidorId is int bastidorId && await bastidorRepository.ObtenerPorIdAsync(bastidorId, ct) is null)
                return Error.NotFound("Bastidor.NoEncontrado", $"El bastidor {bastidorId} no existe.");
        }

        // NOTA: sin transacción compartida entre estas escrituras — ver limitación en docs/arquitectura-tecnica.md sección 3.
        var campañaId = await campañaRepository.CrearAsync(campañaResult.Value, ct);

        var loteIds = new List<int>();
        foreach (var distribucion in distribuciones)
        {
            var codigoLote = await GeneradorCodigoLote.GenerarAsync(loteRepository, fechaSiembra, ct);

            var loteResult = Lote.Crear(
                codigoLote, campañaId, distribucion.CantidadPeces, pesoPromedioInicialGr, fechaSiembra,
                unidadProduccionId: distribucion.UnidadProduccionId, bastidorId: distribucion.BastidorId,
                tallaInicialCm: tallaPromedioInicialCm);
            if (loteResult.IsError)
                return loteResult.Errors;

            var loteId = await loteRepository.CrearAsync(loteResult.Value, ct);
            loteIds.Add(loteId);

            var historialResult = HistorialMovimiento.Crear(
                loteId, fechaSiembra.ToDateTime(TimeOnly.MinValue), TipoMovimientoHistorial.Creacion,
                $"Lote creado por siembra de campaña {codigoCampaña}.", cantidadAfectada: distribucion.CantidadPeces,
                etapaNueva: loteResult.Value.EtapaActual);
            if (!historialResult.IsError)
                await historialRepository.CrearAsync(historialResult.Value, ct);
        }

        return new CrearCampañaResultado(campañaId, loteIds);
    }
}
