using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Application.Usuarios.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default);
    Task<bool> ExisteAlgunUsuarioAsync(CancellationToken ct = default);
    Task<int> CrearAsync(Usuario usuario, CancellationToken ct = default);
    Task ActualizarAsync(Usuario usuario, CancellationToken ct = default);
}
