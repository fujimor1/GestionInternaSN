using SierraNevada.Domain.Inventario;

namespace SierraNevada.Application.Inventario.Repositories;

public interface ITipoAlimentoRepository
{
    Task<IReadOnlyList<TipoAlimentoCatalogo>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<TipoAlimentoCatalogo?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<int> CrearAsync(TipoAlimentoCatalogo tipoAlimento, CancellationToken ct = default);
    Task ActualizarAsync(TipoAlimentoCatalogo tipoAlimento, CancellationToken ct = default);
}
