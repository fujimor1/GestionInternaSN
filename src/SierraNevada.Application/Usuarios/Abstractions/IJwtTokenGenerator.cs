using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Application.Usuarios.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerarToken(Usuario usuario);
}
