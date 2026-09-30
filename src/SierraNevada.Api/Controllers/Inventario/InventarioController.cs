using Microsoft.AspNetCore.Mvc;
using SierraNevada.Application.Inventario.CasosDeUso;
using SierraNevada.Application.Inventario.Repositories;
using SierraNevada.Domain.Inventario;

namespace SierraNevada.Api.Controllers.Inventario;

public sealed record CrearTipoAlimentoRequest(
    string Nombre,
    string Marca,
    decimal CalibreMm,
    decimal PorcentajeProteina,
    decimal PorcentajeGrasa,
    string EtapaSugerida,
    decimal CostoUnitarioPromedioKg,
    decimal CostoAlmacenamientoAnualPorKg = 0.50m);

public sealed record CrearProveedorRequest(
    string Ruc,
    string RazonSocial,
    string? ContactoNombre,
    string? Telefono,
    string? Email,
    int LeadTimeDiasPromedio,
    decimal CostoOrdenPedido);

[ApiController]
[Route("api/inventario")]
public sealed class InventarioController(
    ITipoAlimentoRepository tipoAlimentoRepo,
    IProveedorAlimentoRepository proveedorRepo,
    ILoteAlimentoRepository loteAlimentoRepo,
    IKardexAlimentoRepository kardexRepo,
    RegistrarIngresoAlimentoService ingresoService,
    RegistrarEgresoAlimentoService egresoService) : ApiControllerBase
{
    // --- Catálogo de Alimentos ---
    [HttpGet("tipos")]
    public async Task<IActionResult> ListarTiposAlimento(CancellationToken ct)
    {
        var lista = await tipoAlimentoRepo.ObtenerTodosAsync(ct);
        return Ok(lista);
    }

    [HttpPost("tipos")]
    public async Task<IActionResult> CrearTipoAlimento([FromBody] CrearTipoAlimentoRequest req, CancellationToken ct)
    {
        var entidadResult = TipoAlimentoCatalogo.Crear(
            req.Nombre, req.Marca, req.CalibreMm, req.PorcentajeProteina, req.PorcentajeGrasa,
            req.EtapaSugerida, req.CostoUnitarioPromedioKg, req.CostoAlmacenamientoAnualPorKg);

        if (entidadResult.IsError)
            return ProblemFromErrors(entidadResult.Errors);

        int id = await tipoAlimentoRepo.CrearAsync(entidadResult.Value, ct);
        return Ok(new { id, mensaje = "Tipo de alimento creado exitosamente." });
    }

    // --- Proveedores ---
    [HttpGet("proveedores")]
    public async Task<IActionResult> ListarProveedores(CancellationToken ct)
    {
        var lista = await proveedorRepo.ObtenerTodosAsync(ct);
        return Ok(lista);
    }

    [HttpPost("proveedores")]
    public async Task<IActionResult> CrearProveedor([FromBody] CrearProveedorRequest req, CancellationToken ct)
    {
        var entidadResult = ProveedorAlimento.Crear(
            req.Ruc, req.RazonSocial, req.ContactoNombre, req.Telefono, req.Email,
            req.LeadTimeDiasPromedio, req.CostoOrdenPedido);

        if (entidadResult.IsError)
            return ProblemFromErrors(entidadResult.Errors);

        int id = await proveedorRepo.CrearAsync(entidadResult.Value, ct);
        return Ok(new { id, mensaje = "Proveedor registrado exitosamente." });
    }

    // --- Lotes de Alimento (Sacos en Almacén) ---
    [HttpGet("lotes")]
    public async Task<IActionResult> ListarLotesAlimento([FromQuery] bool soloConStock = false, CancellationToken ct = default)
    {
        var lotes = await loteAlimentoRepo.ObtenerTodosAsync(soloConStock, ct);
        return Ok(lotes);
    }

    [HttpGet("lotes/por-tipo/{tipoId:int}")]
    public async Task<IActionResult> ListarLotesPorTipo(int tipoId, [FromQuery] bool soloConStock = true, CancellationToken ct = default)
    {
        var lotes = await loteAlimentoRepo.ObtenerPorTipoAlimentoAsync(tipoId, soloConStock, ct);
        return Ok(lotes);
    }

    // --- Movimientos y Kardex ---
    [HttpPost("ingreso")]
    public async Task<IActionResult> RegistrarIngresoCompra([FromBody] RegistrarIngresoAlimentoCommand cmd, CancellationToken ct)
    {
        var resultado = await ingresoService.EjecutarAsync(cmd, ct);
        return resultado.Match(Ok, ProblemFromErrors);
    }

    [HttpPost("egreso")]
    public async Task<IActionResult> RegistrarEgresoAlimento([FromBody] RegistrarEgresoAlimentoCommand cmd, CancellationToken ct)
    {
        var resultado = await egresoService.EjecutarAsync(cmd, ct);
        return resultado.Match(Ok, ProblemFromErrors);
    }

    [HttpGet("kardex")]
    public async Task<IActionResult> ConsultarKardex(
        [FromQuery] int? tipoAlimentoId,
        [FromQuery] int? loteAlimentoId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken ct = default)
    {
        var movimientos = await kardexRepo.ObtenerMovimientosAsync(tipoAlimentoId, loteAlimentoId, desde, hasta, ct);
        return Ok(movimientos);
    }
}
