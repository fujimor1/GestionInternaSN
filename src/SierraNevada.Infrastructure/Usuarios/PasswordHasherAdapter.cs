using Microsoft.AspNetCore.Identity;
using SierraNevada.Application.Usuarios.Abstractions;
using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Infrastructure.Usuarios;

/// <summary>
/// Envuelve Microsoft.AspNetCore.Identity.PasswordHasher&lt;TUser&gt; — viene del paquete
/// Microsoft.Extensions.Identity.Core, que NO depende de EF Core ni del resto de Identity
/// (stores, UserManager, etc.), solo trae el algoritmo de hash (PBKDF2), consistente con la
/// decisión de ADO.NET puro (docs/arquitectura-tecnica.md sección 6).
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string HashPassword(Usuario usuario, string password) => _hasher.HashPassword(usuario, password);

    public bool VerificarPassword(Usuario usuario, string passwordHasheado, string passwordIngresado) =>
        _hasher.VerifyHashedPassword(usuario, passwordHasheado, passwordIngresado) != PasswordVerificationResult.Failed;
}
