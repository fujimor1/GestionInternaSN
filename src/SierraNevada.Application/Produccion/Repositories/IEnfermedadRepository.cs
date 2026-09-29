using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface IEnfermedadRepository
{
    Task<Enfermedad?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Enfermedad>> ListarTodasAsync(CancellationToken ct = default);
    Task<int> CrearAsync(Enfermedad enfermedad, CancellationToken ct = default);
}
