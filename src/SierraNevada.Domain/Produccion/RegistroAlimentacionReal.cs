using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Alimento real entregado (kg) — el sistema Django anterior solo tenía un booleano
/// (alimentacion_realizada), sin cantidad. Sin esto, el FCA real nunca se puede calcular
/// (docs/diseno-modulo-produccion.md, gap #2).
/// </summary>
public sealed class RegistroAlimentacionReal
{
    public int Id { get; private set; }
    public int LoteId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public decimal CantidadKgEntregada { get; private set; }
    public TipoAlimento TipoAlimento { get; private set; }

    private RegistroAlimentacionReal() { }

    public static ErrorOr<RegistroAlimentacionReal> Crear(
        int loteId, DateOnly fecha, decimal cantidadKgEntregada, TipoAlimento tipoAlimento)
    {
        if (cantidadKgEntregada <= 0)
            return Error.Validation("RegistroAlimentacionReal.CantidadKgEntregada", "La cantidad entregada debe ser mayor a cero.");

        return new RegistroAlimentacionReal
        {
            LoteId = loteId,
            Fecha = fecha,
            CantidadKgEntregada = cantidadKgEntregada,
            TipoAlimento = tipoAlimento,
        };
    }

    public static RegistroAlimentacionReal Reconstruir(
        int id, int loteId, DateOnly fecha, decimal cantidadKgEntregada, TipoAlimento tipoAlimento)
        => new() { Id = id, LoteId = loteId, Fecha = fecha, CantidadKgEntregada = cantidadKgEntregada, TipoAlimento = tipoAlimento };
}
