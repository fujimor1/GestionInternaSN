using SierraNevada.Domain.Inventario;

namespace SierraNevada.Application.Inventario.Repositories;

public interface IProveedorAlimentoRepository
{
    Task<IReadOnlyList<ProveedorAlimento>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<ProveedorAlimento?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<int> CrearAsync(ProveedorAlimento proveedor, CancellationToken ct = default);
    Task ActualizarAsync(ProveedorAlimento proveedor, CancellationToken ct = default);
}
