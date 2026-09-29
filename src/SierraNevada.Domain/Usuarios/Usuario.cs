using ErrorOr;

namespace SierraNevada.Domain.Usuarios;

/// <summary>
/// Autenticación propia (no ASP.NET Core Identity — incompatible con la decisión de ADO.NET
/// puro sin EF Core, ver docs/arquitectura-tecnica.md sección 6). El hash de contraseña se
/// genera en Application/Infrastructure (esta entidad solo guarda el resultado, nunca la
/// contraseña en texto plano ni sabe cómo se genera el hash).
/// </summary>
public sealed class Usuario
{
    public int Id { get; private set; }
    public string NombreUsuario { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string NombreCompleto { get; private set; } = string.Empty;
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    private Usuario() { }

    /// <summary>
    /// Crea el usuario SIN password hash — el hasher (Infrastructure) necesita la instancia de
    /// Usuario ya construida para generar el hash (Microsoft.AspNetCore.Identity.PasswordHasher
    /// recibe el usuario como parámetro), así que el hash se asigna después con
    /// ActualizarPasswordHash, antes de persistir. Quien orquesta esto (AutenticacionService)
    /// garantiza que nunca se persiste un Usuario sin hash real.
    /// </summary>
    public static ErrorOr<Usuario> Crear(string nombreUsuario, string email, string nombreCompleto, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            return Error.Validation("Usuario.NombreUsuario", "El nombre de usuario es obligatorio.");

        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation("Usuario.Email", "El email es obligatorio.");

        if (string.IsNullOrWhiteSpace(nombreCompleto))
            return Error.Validation("Usuario.NombreCompleto", "El nombre completo es obligatorio.");

        return new Usuario
        {
            NombreUsuario = nombreUsuario,
            Email = email,
            PasswordHash = string.Empty,
            NombreCompleto = nombreCompleto,
            Rol = rol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
        };
    }

    public static Usuario Reconstruir(
        int id, string nombreUsuario, string email, string passwordHash, string nombreCompleto,
        RolUsuario rol, bool activo, DateTime fechaCreacion)
        => new()
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            Email = email,
            PasswordHash = passwordHash,
            NombreCompleto = nombreCompleto,
            Rol = rol,
            Activo = activo,
            FechaCreacion = fechaCreacion,
        };

    public ErrorOr<Success> ActualizarPasswordHash(string nuevoHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoHash))
            return Error.Validation("Usuario.PasswordHash", "El hash de contraseña no puede estar vacío.");

        PasswordHash = nuevoHash;
        return Result.Success;
    }

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;
}
