using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface IRegistroCondicionesRepository
{
    Task<IReadOnlyList<RegistroCondiciones>> ListarPorLoteAsync(int loteId, CancellationToken ct = default);
    Task<int> CrearAsync(RegistroCondiciones registro, CancellationToken ct = default);
}
