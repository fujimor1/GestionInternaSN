using ErrorOr;
using SierraNevada.Application.Usuarios.Abstractions;
using SierraNevada.Application.Usuarios.Repositories;
using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Application.Usuarios;

/// <summary>
/// Orquesta login y el arranque inicial del sistema (creación del primer Administrador).
/// Solo depende de interfaces (IUsuarioRepository, IPasswordHasher, IJwtTokenGenerator) — no
/// conoce ADO.NET ni JWT concretamente, eso vive en Infrastructure.
/// </summary>
public sealed class AutenticacionService(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
{
    public static readonly Error CredencialesInvalidas = Error.Unauthorized(
        "Autenticacion.CredencialesInvalidas", "Usuario o contraseña incorrectos.");

    public static readonly Error UsuarioInactivo = Error.Unauthorized(
        "Autenticacion.UsuarioInactivo", "El usuario está desactivado.");

    public static readonly Error YaExisteUnUsuario = Error.Conflict(
        "Autenticacion.YaExisteUnUsuario", "Ya existe al menos un usuario — el bootstrap solo aplica en la instalación inicial.");

    public async Task<ErrorOr<ResultadoLogin>> IniciarSesionAsync(
        string nombreUsuario, string password, CancellationToken ct = default)
    {
        var usuario = await usuarioRepository.ObtenerPorNombreUsuarioAsync(nombreUsuario, ct);
        if (usuario is null)
            return CredencialesInvalidas;

        if (!usuario.Activo)
            return UsuarioInactivo;

        if (!passwordHasher.VerificarPassword(usuario, usuario.PasswordHash, password))
            return CredencialesInvalidas;

        var token = jwtTokenGenerator.GenerarToken(usuario);
        return new ResultadoLogin(token, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Rol);
    }

    /// <summary>
    /// Crea el primer usuario Administrador. Solo funciona si todavía no existe ningún usuario
    /// en el sistema — evita tener que insertar el primer admin a mano por SQL en el despliegue.
    /// </summary>
    public async Task<ErrorOr<ResultadoLogin>> CrearPrimerAdministradorAsync(
        string nombreUsuario, string email, string password, string nombreCompleto, CancellationToken ct = default)
    {
        if (await usuarioRepository.ExisteAlgunUsuarioAsync(ct))
            return YaExisteUnUsuario;

        var usuarioResult = Usuario.Crear(nombreUsuario, email, nombreCompleto, RolUsuario.Administrador);
        if (usuarioResult.IsError)
            return usuarioResult.Errors;

        var usuario = usuarioResult.Value;
        var hash = passwordHasher.HashPassword(usuario, password);
        var actualizarHashResult = usuario.ActualizarPasswordHash(hash);
        if (actualizarHashResult.IsError)
            return actualizarHashResult.Errors;

        await usuarioRepository.CrearAsync(usuario, ct);

        var token = jwtTokenGenerator.GenerarToken(usuario);
        return new ResultadoLogin(token, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Rol);
    }
}
