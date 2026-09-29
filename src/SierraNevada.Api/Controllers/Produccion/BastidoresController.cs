using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Api.Controllers.Produccion;

public sealed record CrearBastidorRequest(string Codigo, int CapacidadMaximaUnidades);
public sealed record ActualizarBastidorRequest(string Codigo, int CapacidadMaximaUnidades);

[ApiController]
[Route("api/produccion/bastidores")]
[Authorize]
public sealed class BastidoresController(IBastidorRepository bastidorRepository) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListarDisponibles(CancellationToken ct)
        => Ok(await bastidorRepository.ListarDisponiblesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct)
    {
        var bastidor = await bastidorRepository.ObtenerPorIdAsync(id, ct);
        return bastidor is null ? NotFound() : Ok(bastidor);
    }

    [HttpPost]
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    public async Task<IActionResult> Crear(CrearBastidorRequest request, CancellationToken ct)
    {
        var resultado = Bastidor.Crear(request.Codigo, request.CapacidadMaximaUnidades);
        if (resultado.IsError)
            return ProblemFromErrors(resultado.Errors);

        var id = await bastidorRepository.CrearAsync(resultado.Value, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    public async Task<IActionResult> Actualizar(int id, ActualizarBastidorRequest request, CancellationToken ct)
    {
        var bastidor = await bastidorRepository.ObtenerPorIdAsync(id, ct);
        if (bastidor is null)
            return NotFound();

        var resultado = bastidor.Actualizar(request.Codigo, request.CapacidadMaximaUnidades);
        if (resultado.IsError)
            return ProblemFromErrors(resultado.Errors);

        await bastidorRepository.ActualizarAsync(bastidor, ct);
        return NoContent();
    }
}
