using ErrorOr;

namespace SierraNevada.Domain.Produccion;

public sealed class RegistroMortalidad
{
    public int Id { get; private set; }
    public int LoteId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public int Cantidad { get; private set; }
    public string RegistradoPor { get; private set; } = string.Empty;

    private RegistroMortalidad() { }

    public static ErrorOr<RegistroMortalidad> Crear(int loteId, DateOnly fecha, int cantidad, string registradoPor)
    {
        if (cantidad <= 0)
            return Error.Validation("RegistroMortalidad.Cantidad", "La cantidad debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(registradoPor))
            return Error.Validation("RegistroMortalidad.RegistradoPor", "Debe indicarse quién registró la mortalidad.");

        return new RegistroMortalidad { LoteId = loteId, Fecha = fecha, Cantidad = cantidad, RegistradoPor = registradoPor };
    }

    public static RegistroMortalidad Reconstruir(int id, int loteId, DateOnly fecha, int cantidad, string registradoPor)
        => new() { Id = id, LoteId = loteId, Fecha = fecha, Cantidad = cantidad, RegistradoPor = registradoPor };
}
