using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Usuarios;

namespace SierraNevada.Api.Controllers.Usuarios;

public sealed record LoginRequest(string NombreUsuario, string Password);

public sealed record BootstrapRequest(string NombreUsuario, string Email, string Password, string NombreCompleto);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AutenticacionService autenticacionService) : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var resultado = await autenticacionService.IniciarSesionAsync(request.NombreUsuario, request.Password, ct);
        return resultado.Match(Ok, ProblemFromErrors);
    }

    /// <summary>
    /// Crea el primer usuario Administrador. Solo funciona una vez (mientras no exista ningún
    /// usuario en el sistema) — evita insertar el primer admin a mano por SQL en el despliegue.
    /// </summary>
    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap(BootstrapRequest request, CancellationToken ct)
    {
        var resultado = await autenticacionService.CrearPrimerAdministradorAsync(
            request.NombreUsuario, request.Email, request.Password, request.NombreCompleto, ct);
        return resultado.Match(Ok, ProblemFromErrors);
    }
}
