using SierraNevada.Domain.Inventario;

namespace SierraNevada.Application.Inventario.Repositories;

public interface ILoteAlimentoRepository
{
    Task<IReadOnlyList<LoteAlimento>> ObtenerTodosAsync(bool soloConStock = false, CancellationToken ct = default);
    Task<IReadOnlyList<LoteAlimento>> ObtenerPorTipoAlimentoAsync(int tipoAlimentoId, bool soloConStock = true, CancellationToken ct = default);
    Task<LoteAlimento?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<int> CrearAsync(LoteAlimento lote, CancellationToken ct = default);
    Task ActualizarStockAsync(LoteAlimento lote, CancellationToken ct = default);
}
