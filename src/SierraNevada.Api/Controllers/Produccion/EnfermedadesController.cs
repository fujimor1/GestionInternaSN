using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Api.Controllers.Produccion;

public sealed record CrearEnfermedadRequest(string Nombre, string Descripcion, string Tratamiento, string Prevencion);

/// <summary>Catálogo informativo de enfermedades — sin relación directa a Lote.</summary>
[ApiController]
[Route("api/produccion/enfermedades")]
[Authorize]
public sealed class EnfermedadesController(IEnfermedadRepository enfermedadRepository) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await enfermedadRepository.ListarTodasAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct)
    {
        var enfermedad = await enfermedadRepository.ObtenerPorIdAsync(id, ct);
        return enfermedad is null ? NotFound() : Ok(enfermedad);
    }

    [HttpPost]
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    public async Task<IActionResult> Crear(CrearEnfermedadRequest request, CancellationToken ct)
    {
        var resultado = Enfermedad.Crear(request.Nombre, request.Descripcion, request.Tratamiento, request.Prevencion);
        if (resultado.IsError)
            return ProblemFromErrors(resultado.Errors);

        var id = await enfermedadRepository.CrearAsync(resultado.Value, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id }, new { id });
    }
}
