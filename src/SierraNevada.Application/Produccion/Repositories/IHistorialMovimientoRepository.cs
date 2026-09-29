using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface IHistorialMovimientoRepository
{
    /// <summary>Historial completo de un lote, más reciente primero.</summary>
    Task<IReadOnlyList<HistorialMovimiento>> ListarPorLoteAsync(int loteId, CancellationToken ct = default);

    /// <summary>
    /// Eventos que marcan entrada a una etapa (Creacion inicial + CambioEtapa posteriores),
    /// ordenados por fecha — para calcular duración real por etapa (docs/diseno-modulo-produccion.md 4.5/5).
    /// </summary>
    Task<IReadOnlyList<HistorialMovimiento>> ListarEventosEtapaPorLoteAsync(int loteId, CancellationToken ct = default);

    Task<int> CrearAsync(HistorialMovimiento movimiento, CancellationToken ct = default);
}
