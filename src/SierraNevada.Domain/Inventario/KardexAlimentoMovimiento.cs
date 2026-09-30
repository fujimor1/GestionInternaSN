using ErrorOr;

namespace SierraNevada.Domain.Inventario;

public sealed class KardexAlimentoMovimiento
{
    public int Id { get; private set; }
    public int TipoAlimentoId { get; private set; }
    public int LoteAlimentoId { get; private set; }
    public DateTime FechaMovimiento { get; private set; }
    public TipoMovimientoKardex TipoMovimiento { get; private set; }
    public decimal CantidadKg { get; private set; }
    public decimal CostoUnitarioKg { get; private set; }
    public decimal CostoTotal { get; private set; }
    public decimal SaldoStockKg { get; private set; }
    public decimal SaldoValorizado { get; private set; }
    public int? LoteProduccionId { get; private set; }
    public string? Observaciones { get; private set; }
    public int? UsuarioId { get; private set; }

    private KardexAlimentoMovimiento() { }

    public static ErrorOr<KardexAlimentoMovimiento> Registrar(
        int tipoAlimentoId,
        int loteAlimentoId,
        TipoMovimientoKardex tipoMovimiento,
        decimal cantidadKg,
        decimal costoUnitarioKg,
        decimal saldoStockAnteriorKg,
        decimal saldoValorizadoAnterior,
        int? loteProduccionId = null,
        string? observaciones = null,
        int? usuarioId = null)
    {
        if (cantidadKg == 0)
            return Error.Validation("KardexAlimentoMovimiento.CantidadKg", "La cantidad en kg no puede ser cero.");

        decimal costoTotal = Math.Abs(cantidadKg) * costoUnitarioKg;
        decimal nuevoSaldoKg = saldoStockAnteriorKg + cantidadKg;

        if (nuevoSaldoKg < 0)
            return Error.Validation("KardexAlimentoMovimiento.SaldoNegativo", "El movimiento resultaría en un saldo de stock negativo.");

        decimal nuevoSaldoValorizado = tipoMovimiento switch
        {
            TipoMovimientoKardex.IngresoCompra => saldoValorizadoAnterior + costoTotal,
            TipoMovimientoKardex.Devolucion => saldoValorizadoAnterior + costoTotal,
            TipoMovimientoKardex.EgresoAlimentacion => saldoValorizadoAnterior - costoTotal,
            TipoMovimientoKardex.AjusteMerma => saldoValorizadoAnterior - costoTotal,
            _ => saldoValorizadoAnterior + (cantidadKg * costoUnitarioKg)
        };

        if (nuevoSaldoValorizado < 0)
            nuevoSaldoValorizado = 0;

        return new KardexAlimentoMovimiento
        {
            TipoAlimentoId = tipoAlimentoId,
            LoteAlimentoId = loteAlimentoId,
            FechaMovimiento = DateTime.UtcNow,
            TipoMovimiento = tipoMovimiento,
            CantidadKg = cantidadKg,
            CostoUnitarioKg = costoUnitarioKg,
            CostoTotal = costoTotal,
            SaldoStockKg = nuevoSaldoKg,
            SaldoValorizado = nuevoSaldoValorizado,
            LoteProduccionId = loteProduccionId,
            Observaciones = observaciones?.Trim(),
            UsuarioId = usuarioId
        };
    }

    public static KardexAlimentoMovimiento Reconstruir(
        int id,
        int tipoAlimentoId,
        int loteAlimentoId,
        DateTime fechaMovimiento,
        TipoMovimientoKardex tipoMovimiento,
        decimal cantidadKg,
        decimal costoUnitarioKg,
        decimal costoTotal,
        decimal saldoStockKg,
        decimal saldoValorizado,
        int? loteProduccionId,
        string? observaciones,
        int? usuarioId)
    {
        return new KardexAlimentoMovimiento
        {
            Id = id,
            TipoAlimentoId = tipoAlimentoId,
            LoteAlimentoId = loteAlimentoId,
            FechaMovimiento = fechaMovimiento,
            TipoMovimiento = tipoMovimiento,
            CantidadKg = cantidadKg,
            CostoUnitarioKg = costoUnitarioKg,
            CostoTotal = costoTotal,
            SaldoStockKg = saldoStockKg,
            SaldoValorizado = saldoValorizado,
            LoteProduccionId = loteProduccionId,
            Observaciones = observaciones,
            UsuarioId = usuarioId
        };
    }
}
