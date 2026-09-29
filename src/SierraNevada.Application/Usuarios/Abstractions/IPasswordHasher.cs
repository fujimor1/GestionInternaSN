using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Application.Usuarios.Abstractions;

/// <summary>Puerto hacia el hasher de contraseñas — la implementación (Infrastructure) usa
/// Microsoft.AspNetCore.Identity.PasswordHasher, sin traer todo ASP.NET Core Identity.</summary>
public interface IPasswordHasher
{
    string HashPassword(Usuario usuario, string password);
    bool VerificarPassword(Usuario usuario, string passwordHasheado, string passwordIngresado);
}
