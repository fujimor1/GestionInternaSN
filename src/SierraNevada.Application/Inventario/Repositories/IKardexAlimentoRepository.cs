using SierraNevada.Domain.Inventario;

namespace SierraNevada.Application.Inventario.Repositories;

public sealed record KardexResumenSaldo(decimal TotalKg, decimal TotalValorizado);

public sealed record ConsumoHistoricoDiario(DateOnly Fecha, decimal TotalKg);

public interface IKardexAlimentoRepository
{
    Task<IReadOnlyList<KardexAlimentoMovimiento>> ObtenerMovimientosAsync(
        int? tipoAlimentoId = null,
        int? loteAlimentoId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken ct = default);

    Task<KardexResumenSaldo> ObtenerSaldoActualPorTipoAlimentoAsync(int tipoAlimentoId, CancellationToken ct = default);

    Task<IReadOnlyList<ConsumoHistoricoDiario>> ObtenerConsumoDiarioHistoricoAsync(
        int? tipoAlimentoId = null,
        int diasAtras = 90,
        CancellationToken ct = default);

    Task<int> RegistrarMovimientoAsync(KardexAlimentoMovimiento movimiento, CancellationToken ct = default);
}
