using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>Catálogo de enfermedades — referencia informativa, sin relación directa a Lote (igual que en el sistema anterior).</summary>
public sealed class Enfermedad
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;
    public string Tratamiento { get; private set; } = string.Empty;
    public string Prevencion { get; private set; } = string.Empty;

    private Enfermedad() { }

    public static ErrorOr<Enfermedad> Crear(string nombre, string descripcion, string tratamiento, string prevencion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Error.Validation("Enfermedad.Nombre", "El nombre es obligatorio.");

        return new Enfermedad { Nombre = nombre, Descripcion = descripcion, Tratamiento = tratamiento, Prevencion = prevencion };
    }

    public static Enfermedad Reconstruir(int id, string nombre, string descripcion, string tratamiento, string prevencion)
        => new() { Id = id, Nombre = nombre, Descripcion = descripcion, Tratamiento = tratamiento, Prevencion = prevencion };
}
