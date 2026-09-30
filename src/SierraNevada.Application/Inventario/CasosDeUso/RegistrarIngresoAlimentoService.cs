using ErrorOr;
using SierraNevada.Application.Inventario.Repositories;
using SierraNevada.Domain.Inventario;

namespace SierraNevada.Application.Inventario.CasosDeUso;

public sealed record RegistrarIngresoAlimentoCommand(
    int TipoAlimentoId,
    int ProveedorId,
    string CodigoLoteFabrica,
    DateOnly? FechaFabricacion,
    DateOnly FechaVencimiento,
    decimal PesoPorSacoKg,
    int CantidadSacos,
    decimal PrecioUnitarioKg,
    string? Observaciones = null,
    int? UsuarioId = null);

public sealed record IngresoAlimentoResponse(int LoteAlimentoId, int KardexMovimientoId, decimal TotalKg, decimal TotalCosto);

public sealed class RegistrarIngresoAlimentoService(
    ITipoAlimentoRepository tipoAlimentoRepo,
    IProveedorAlimentoRepository proveedorRepo,
    ILoteAlimentoRepository loteAlimentoRepo,
    IKardexAlimentoRepository kardexRepo)
{
    public async Task<ErrorOr<IngresoAlimentoResponse>> EjecutarAsync(RegistrarIngresoAlimentoCommand cmd, CancellationToken ct = default)
    {
        var tipoAlimento = await tipoAlimentoRepo.ObtenerPorIdAsync(cmd.TipoAlimentoId, ct);
        if (tipoAlimento is null)
            return Error.NotFound("TipoAlimento.NotFound", $"El tipo de alimento con ID {cmd.TipoAlimentoId} no existe.");

        var proveedor = await proveedorRepo.ObtenerPorIdAsync(cmd.ProveedorId, ct);
        if (proveedor is null)
            return Error.NotFound("Proveedor.NotFound", $"El proveedor con ID {cmd.ProveedorId} no existe.");

        var loteResult = LoteAlimento.Crear(
            cmd.TipoAlimentoId,
            cmd.ProveedorId,
            cmd.CodigoLoteFabrica,
            cmd.FechaFabricacion,
            cmd.FechaVencimiento,
            cmd.PesoPorSacoKg,
            cmd.CantidadSacos,
            cmd.PrecioUnitarioKg);

        if (loteResult.IsError)
            return loteResult.Errors;

        var lote = loteResult.Value;
        int loteId = await loteAlimentoRepo.CrearAsync(lote, ct);

        // Obtener saldo anterior de kardex para este tipo de alimento
        var saldoAnterior = await kardexRepo.ObtenerSaldoActualPorTipoAlimentoAsync(cmd.TipoAlimentoId, ct);
        decimal cantidadTotalKg = cmd.CantidadSacos * cmd.PesoPorSacoKg;

        var movimientoResult = KardexAlimentoMovimiento.Registrar(
            cmd.TipoAlimentoId,
            loteId,
            TipoMovimientoKardex.IngresoCompra,
            cantidadTotalKg,
            cmd.PrecioUnitarioKg,
            saldoAnterior.TotalKg,
            saldoAnterior.TotalValorizado,
            null,
            cmd.Observaciones ?? $"Ingreso por compra - Lote Fábrica {cmd.CodigoLoteFabrica} ({cmd.CantidadSacos} sacos)",
            cmd.UsuarioId);

        if (movimientoResult.IsError)
            return movimientoResult.Errors;

        int kardexId = await kardexRepo.RegistrarMovimientoAsync(movimientoResult.Value, ct);

        // Actualizar costo promedio referencial en el tipo de alimento
        tipoAlimento.ActualizarCostoUnitario(cmd.PrecioUnitarioKg);
        await tipoAlimentoRepo.ActualizarAsync(tipoAlimento, ct);

        return new IngresoAlimentoResponse(loteId, kardexId, cantidadTotalKg, cantidadTotalKg * cmd.PrecioUnitarioKg);
    }
}
