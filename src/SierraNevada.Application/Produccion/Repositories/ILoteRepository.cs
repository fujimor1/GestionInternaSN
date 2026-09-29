using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface ILoteRepository
{
    Task<Lote?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Lote?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default);
    Task<int> ContarPorPrefijoCodigoAsync(string prefijo, CancellationToken ct = default);
    Task<IReadOnlyList<Lote>> ListarPorCampañaAsync(int campañaId, CancellationToken ct = default);
    Task<IReadOnlyList<Lote>> ListarHijosAsync(int lotePadreId, CancellationToken ct = default);
    Task<IReadOnlyList<Lote>> ListarActivosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Lote>> ListarPorUnidadProduccionAsync(int unidadProduccionId, CancellationToken ct = default);
    Task<int> CrearAsync(Lote lote, CancellationToken ct = default);
    Task ActualizarAsync(Lote lote, CancellationToken ct = default);
}
