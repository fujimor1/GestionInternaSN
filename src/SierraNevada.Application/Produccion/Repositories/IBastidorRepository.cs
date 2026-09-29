using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface IBastidorRepository
{
    Task<Bastidor?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Bastidor>> ListarDisponiblesAsync(CancellationToken ct = default);
    Task<int> CrearAsync(Bastidor bastidor, CancellationToken ct = default);
    Task ActualizarAsync(Bastidor bastidor, CancellationToken ct = default);
}
