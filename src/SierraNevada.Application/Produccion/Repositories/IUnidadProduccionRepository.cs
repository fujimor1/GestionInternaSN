using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface IUnidadProduccionRepository
{
    Task<UnidadProduccion?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<UnidadProduccion?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default);
    Task<IReadOnlyList<UnidadProduccion>> ListarTodasAsync(CancellationToken ct = default);
    Task<int> CrearAsync(UnidadProduccion unidad, CancellationToken ct = default);
    Task ActualizarAsync(UnidadProduccion unidad, CancellationToken ct = default);
}
