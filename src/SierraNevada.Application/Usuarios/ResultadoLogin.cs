using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Application.Usuarios;

public sealed record ResultadoLogin(string Token, string NombreUsuario, string NombreCompleto, RolUsuario Rol);
