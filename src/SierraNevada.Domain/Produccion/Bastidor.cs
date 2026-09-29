using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>Bastidor de incubación de ovas — etapa previa a la siembra en Jaula/Artesa.</summary>
public sealed class Bastidor
{
    public int Id { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public int CapacidadMaximaUnidades { get; private set; }
    public bool EstaDisponible { get; private set; }

    private Bastidor() { }

    public static ErrorOr<Bastidor> Crear(string codigo, int capacidadMaximaUnidades)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Error.Validation("Bastidor.Codigo", "El código es obligatorio.");

        if (capacidadMaximaUnidades <= 0)
            return Error.Validation("Bastidor.CapacidadMaximaUnidades", "La capacidad debe ser mayor a cero.");

        return new Bastidor
        {
            Codigo = codigo,
            CapacidadMaximaUnidades = capacidadMaximaUnidades,
            EstaDisponible = true,
        };
    }

    public static Bastidor Reconstruir(int id, string codigo, int capacidadMaximaUnidades, bool estaDisponible)
        => new() { Id = id, Codigo = codigo, CapacidadMaximaUnidades = capacidadMaximaUnidades, EstaDisponible = estaDisponible };

    public ErrorOr<Success> Actualizar(string codigo, int capacidadMaximaUnidades)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Error.Validation("Bastidor.Codigo", "El código es obligatorio.");

        if (capacidadMaximaUnidades <= 0)
            return Error.Validation("Bastidor.CapacidadMaximaUnidades", "La capacidad debe ser mayor a cero.");

        Codigo = codigo;
        CapacidadMaximaUnidades = capacidadMaximaUnidades;
        return Result.Success;
    }

    public void MarcarOcupado() => EstaDisponible = false;

    public void MarcarDisponible() => EstaDisponible = true;
}
