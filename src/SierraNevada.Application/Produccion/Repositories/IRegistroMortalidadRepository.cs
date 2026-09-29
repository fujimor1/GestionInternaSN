using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface IRegistroMortalidadRepository
{
    Task<IReadOnlyList<RegistroMortalidad>> ListarPorLoteAsync(int loteId, CancellationToken ct = default);

    Task<IReadOnlyList<RegistroMortalidad>> ListarPorLoteEnRangoAsync(
        int loteId, DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    Task<int> CrearAsync(RegistroMortalidad registro, CancellationToken ct = default);
}
