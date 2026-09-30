using ErrorOr;
using SierraNevada.Application.Inventario.Repositories;
using SierraNevada.Domain.Inventario;

namespace SierraNevada.Application.Inventario.CasosDeUso;

public sealed record RegistrarEgresoAlimentoCommand(
    int LoteAlimentoId,
    TipoMovimientoKardex TipoMovimiento,
    decimal CantidadKg,
    int? LoteProduccionId = null,
    string? Observaciones = null,
    int? UsuarioId = null);

public sealed record EgresoAlimentoResponse(int KardexMovimientoId, decimal StockRestanteKg, decimal CostoTotalEgreso);

public sealed class RegistrarEgresoAlimentoService(
    ILoteAlimentoRepository loteAlimentoRepo,
    IKardexAlimentoRepository kardexRepo)
{
    public async Task<ErrorOr<EgresoAlimentoResponse>> EjecutarAsync(RegistrarEgresoAlimentoCommand cmd, CancellationToken ct = default)
    {
        var lote = await loteAlimentoRepo.ObtenerPorIdAsync(cmd.LoteAlimentoId, ct);
        if (lote is null)
            return Error.NotFound("LoteAlimento.NotFound", $"El lote de alimento con ID {cmd.LoteAlimentoId} no existe.");

        var descuentoResult = lote.DescontarStock(cmd.CantidadKg);
        if (descuentoResult.IsError)
            return descuentoResult.Errors;

        await loteAlimentoRepo.ActualizarStockAsync(lote, ct);

        var saldoAnterior = await kardexRepo.ObtenerSaldoActualPorTipoAlimentoAsync(lote.TipoAlimentoId, ct);

        // Movimiento con cantidad negativa para egreso
        var movimientoResult = KardexAlimentoMovimiento.Registrar(
            lote.TipoAlimentoId,
            lote.Id,
            cmd.TipoMovimiento,
            -cmd.CantidadKg,
            lote.PrecioUnitarioKg,
            saldoAnterior.TotalKg,
            saldoAnterior.TotalValorizado,
            cmd.LoteProduccionId,
            cmd.Observaciones ?? $"Egreso de alimento - Lote Fábrica {lote.CodigoLoteFabrica}",
            cmd.UsuarioId);

        if (movimientoResult.IsError)
            return movimientoResult.Errors;

        int kardexId = await kardexRepo.RegistrarMovimientoAsync(movimientoResult.Value, ct);

        return new EgresoAlimentoResponse(kardexId, lote.StockKgActual, cmd.CantidadKg * lote.PrecioUnitarioKg);
    }
}
