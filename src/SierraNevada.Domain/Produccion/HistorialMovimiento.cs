using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Log de eventos por lote. Extiende el HistorialMovimiento del sistema Django anterior con
/// el tipo CambioEtapa (con EtapaAnterior/EtapaNueva) — de ahí sale la duración real por etapa
/// ("cuánto se demoró de alevines 1 a alevines 2"), restando fechas entre dos eventos
/// CambioEtapa consecutivos del mismo lote (docs/diseno-modulo-produccion.md, sección 4.5).
/// </summary>
public sealed class HistorialMovimiento
{
    public int Id { get; private set; }
    public int LoteId { get; private set; }
    public DateTime Fecha { get; private set; }
    public TipoMovimientoHistorial TipoMovimiento { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public int? CantidadAfectada { get; private set; }
    public EtapaProductiva? EtapaAnterior { get; private set; }
    public EtapaProductiva? EtapaNueva { get; private set; }

    private HistorialMovimiento() { }

    public static ErrorOr<HistorialMovimiento> Crear(
        int loteId, DateTime fecha, TipoMovimientoHistorial tipoMovimiento, string descripcion,
        int? cantidadAfectada = null, EtapaProductiva? etapaAnterior = null, EtapaProductiva? etapaNueva = null)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return Error.Validation("HistorialMovimiento.Descripcion", "La descripción es obligatoria.");

        if (tipoMovimiento == TipoMovimientoHistorial.CambioEtapa && (etapaAnterior is null || etapaNueva is null))
            return Error.Validation("HistorialMovimiento.Etapas", "Un cambio de etapa requiere etapa anterior y nueva.");

        return new HistorialMovimiento
        {
            LoteId = loteId,
            Fecha = fecha,
            TipoMovimiento = tipoMovimiento,
            Descripcion = descripcion,
            CantidadAfectada = cantidadAfectada,
            EtapaAnterior = etapaAnterior,
            EtapaNueva = etapaNueva,
        };
    }

    public static HistorialMovimiento Reconstruir(
        int id, int loteId, DateTime fecha, TipoMovimientoHistorial tipoMovimiento, string descripcion,
        int? cantidadAfectada, EtapaProductiva? etapaAnterior, EtapaProductiva? etapaNueva)
        => new()
        {
            Id = id,
            LoteId = loteId,
            Fecha = fecha,
            TipoMovimiento = tipoMovimiento,
            Descripcion = descripcion,
            CantidadAfectada = cantidadAfectada,
            EtapaAnterior = etapaAnterior,
            EtapaNueva = etapaNueva,
        };
}
