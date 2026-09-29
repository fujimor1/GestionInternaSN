using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface ICampañaRepository
{
    Task<Campaña?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Campaña?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default);
    Task<IReadOnlyList<Campaña>> ListarTodasAsync(CancellationToken ct = default);
    Task<int> CrearAsync(Campaña campaña, CancellationToken ct = default);
}
