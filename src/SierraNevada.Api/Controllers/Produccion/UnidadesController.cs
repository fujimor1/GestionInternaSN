using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Domain.Usuarios;

namespace SierraNevada.Api.Controllers.Produccion;

public sealed record CrearUnidadRequest(
    string Codigo, TipoUnidadProduccion Tipo, TipoJaula? SubTipoJaula, FormaUnidad Forma,
    decimal? LargoM, decimal? AnchoM, decimal? DiametroM, decimal AltoM, decimal DensidadSiembraKgM3);

public sealed record ActualizarUnidadRequest(
    string Codigo, TipoJaula? SubTipoJaula, FormaUnidad Forma,
    decimal? LargoM, decimal? AnchoM, decimal? DiametroM, decimal AltoM, decimal DensidadSiembraKgM3);

/// <summary>Jaulas y artesas. CRUD simple sin orquestación de negocio — el controller habla directo con el repositorio (Application), sin un caso de uso intermedio.</summary>
[ApiController]
[Route("api/produccion/unidades")]
[Authorize]
public sealed class UnidadesController(IUnidadProduccionRepository unidadRepository) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await unidadRepository.ListarTodasAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct)
    {
        var unidad = await unidadRepository.ObtenerPorIdAsync(id, ct);
        return unidad is null ? NotFound() : Ok(unidad);
    }

    [HttpPost]
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    public async Task<IActionResult> Crear(CrearUnidadRequest request, CancellationToken ct)
    {
        var resultado = UnidadProduccion.Crear(
            request.Codigo, request.Tipo, request.SubTipoJaula, request.Forma,
            request.LargoM, request.AnchoM, request.DiametroM, request.AltoM, request.DensidadSiembraKgM3);

        if (resultado.IsError)
            return ProblemFromErrors(resultado.Errors);

        var id = await unidadRepository.CrearAsync(resultado.Value, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    public async Task<IActionResult> Actualizar(int id, ActualizarUnidadRequest request, CancellationToken ct)
    {
        var unidad = await unidadRepository.ObtenerPorIdAsync(id, ct);
        if (unidad is null)
            return NotFound();

        var resultado = unidad.Actualizar(
            request.Codigo, request.SubTipoJaula, request.Forma,
            request.LargoM, request.AnchoM, request.DiametroM, request.AltoM, request.DensidadSiembraKgM3);

        if (resultado.IsError)
            return ProblemFromErrors(resultado.Errors);

        await unidadRepository.ActualizarAsync(unidad, ct);
        return NoContent();
    }
}
