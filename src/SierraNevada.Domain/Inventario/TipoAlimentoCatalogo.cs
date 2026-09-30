using ErrorOr;

namespace SierraNevada.Domain.Inventario;

public sealed class TipoAlimentoCatalogo
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Marca { get; private set; } = string.Empty;
    public decimal CalibreMm { get; private set; }
    public decimal PorcentajeProteina { get; private set; }
    public decimal PorcentajeGrasa { get; private set; }
    public string EtapaSugerida { get; private set; } = string.Empty;
    public decimal CostoUnitarioPromedioKg { get; private set; }
    public decimal CostoAlmacenamientoAnualPorKg { get; private set; }
    public bool Activo { get; private set; } = true;
    public DateTime FechaCreacion { get; private set; }

    private TipoAlimentoCatalogo() { }

    public static ErrorOr<TipoAlimentoCatalogo> Crear(
        string nombre,
        string marca,
        decimal calibreMm,
        decimal porcentajeProteina,
        decimal porcentajeGrasa,
        string etapaSugerida,
        decimal costoUnitarioPromedioKg,
        decimal costoAlmacenamientoAnualPorKg = 0.50m)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Error.Validation("TipoAlimentoCatalogo.Nombre", "El nombre no puede estar vacío.");

        if (calibreMm <= 0)
            return Error.Validation("TipoAlimentoCatalogo.CalibreMm", "El calibre debe ser mayor a 0 mm.");

        return new TipoAlimentoCatalogo
        {
            Nombre = nombre.Trim(),
            Marca = marca.Trim(),
            CalibreMm = calibreMm,
            PorcentajeProteina = porcentajeProteina,
            PorcentajeGrasa = porcentajeGrasa,
            EtapaSugerida = etapaSugerida.Trim(),
            CostoUnitarioPromedioKg = costoUnitarioPromedioKg,
            CostoAlmacenamientoAnualPorKg = costoAlmacenamientoAnualPorKg <= 0 ? 0.50m : costoAlmacenamientoAnualPorKg,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    public static TipoAlimentoCatalogo Reconstruir(
        int id,
        string nombre,
        string marca,
        decimal calibreMm,
        decimal porcentajeProteina,
        decimal porcentajeGrasa,
        string etapaSugerida,
        decimal costoUnitarioPromedioKg,
        decimal costoAlmacenamientoAnualPorKg,
        bool activo,
        DateTime fechaCreacion)
    {
        return new TipoAlimentoCatalogo
        {
            Id = id,
            Nombre = nombre,
            Marca = marca,
            CalibreMm = calibreMm,
            PorcentajeProteina = porcentajeProteina,
            PorcentajeGrasa = porcentajeGrasa,
            EtapaSugerida = etapaSugerida,
            CostoUnitarioPromedioKg = costoUnitarioPromedioKg,
            CostoAlmacenamientoAnualPorKg = costoAlmacenamientoAnualPorKg,
            Activo = activo,
            FechaCreacion = fechaCreacion
        };
    }

    public void ActualizarCostoUnitario(decimal nuevoCosto)
    {
        if (nuevoCosto > 0)
            CostoUnitarioPromedioKg = nuevoCosto;
    }
}
