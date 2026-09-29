using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface IMuestreoRepository
{
    Task<Muestreo?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>Serie de tiempo completa de un lote, ordenada por fecha — la base del motor de calibración.</summary>
    Task<IReadOnlyList<Muestreo>> ListarPorLoteAsync(int loteId, CancellationToken ct = default);

    Task<Muestreo?> ObtenerUltimoPorLoteAsync(int loteId, CancellationToken ct = default);
    Task<int> CrearAsync(Muestreo muestreo, CancellationToken ct = default);
}
