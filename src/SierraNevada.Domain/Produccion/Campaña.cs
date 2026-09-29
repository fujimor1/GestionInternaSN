using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Evento de siembra — el nivel que faltaba por encima de Lote (docs/diseno-modulo-produccion.md,
/// sección 4.1). Un mismo origen (misma fecha, mismo lote de alevines) puede repartirse en
/// varias unidades de producción desde el inicio; todos los Lote resultantes comparten CampañaId.
/// </summary>
public sealed class Campaña
{
    public int Id { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public DateOnly FechaSiembra { get; private set; }
    public int CantidadAlevinesSembrados { get; private set; }
    public decimal PesoPromedioInicialGr { get; private set; }
    public decimal TallaPromedioInicialCm { get; private set; }
    public string? Proveedor { get; private set; }
    public string? Observaciones { get; private set; }

    private Campaña() { }

    public static ErrorOr<Campaña> Crear(
        string codigo,
        DateOnly fechaSiembra,
        int cantidadAlevinesSembrados,
        decimal pesoPromedioInicialGr,
        decimal tallaPromedioInicialCm,
        string? proveedor,
        string? observaciones)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Error.Validation("Campaña.Codigo", "El código es obligatorio.");

        if (cantidadAlevinesSembrados <= 0)
            return Error.Validation("Campaña.CantidadAlevinesSembrados", "La cantidad sembrada debe ser mayor a cero.");

        if (pesoPromedioInicialGr <= 0)
            return Error.Validation("Campaña.PesoPromedioInicialGr", "El peso inicial debe ser mayor a cero.");

        if (tallaPromedioInicialCm <= 0)
            return Error.Validation("Campaña.TallaPromedioInicialCm", "La talla inicial debe ser mayor a cero.");

        return new Campaña
        {
            Codigo = codigo,
            FechaSiembra = fechaSiembra,
            CantidadAlevinesSembrados = cantidadAlevinesSembrados,
            PesoPromedioInicialGr = pesoPromedioInicialGr,
            TallaPromedioInicialCm = tallaPromedioInicialCm,
            Proveedor = proveedor,
            Observaciones = observaciones,
        };
    }

    public static Campaña Reconstruir(
        int id, string codigo, DateOnly fechaSiembra, int cantidadAlevinesSembrados,
        decimal pesoPromedioInicialGr, decimal tallaPromedioInicialCm, string? proveedor, string? observaciones)
        => new()
        {
            Id = id,
            Codigo = codigo,
            FechaSiembra = fechaSiembra,
            CantidadAlevinesSembrados = cantidadAlevinesSembrados,
            PesoPromedioInicialGr = pesoPromedioInicialGr,
            TallaPromedioInicialCm = tallaPromedioInicialCm,
            Proveedor = proveedor,
            Observaciones = observaciones,
        };
}
