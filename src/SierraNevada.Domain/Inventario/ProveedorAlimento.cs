using ErrorOr;

namespace SierraNevada.Domain.Inventario;

public sealed class ProveedorAlimento
{
    public int Id { get; private set; }
    public string Ruc { get; private set; } = string.Empty;
    public string RazonSocial { get; private set; } = string.Empty;
    public string? ContactoNombre { get; private set; }
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public int LeadTimeDiasPromedio { get; private set; } = 5;
    public decimal CostoOrdenPedido { get; private set; } = 50.00m;
    public bool Activo { get; private set; } = true;
    public DateTime FechaCreacion { get; private set; }

    private ProveedorAlimento() { }

    public static ErrorOr<ProveedorAlimento> Crear(
        string ruc,
        string razonSocial,
        string? contactoNombre,
        string? telefono,
        string? email,
        int leadTimeDiasPromedio,
        decimal costoOrdenPedido)
    {
        if (string.IsNullOrWhiteSpace(ruc) || ruc.Trim().Length < 8)
            return Error.Validation("ProveedorAlimento.Ruc", "El RUC o documento es inválido.");

        if (string.IsNullOrWhiteSpace(razonSocial))
            return Error.Validation("ProveedorAlimento.RazonSocial", "La razón social no puede estar vacía.");

        if (leadTimeDiasPromedio <= 0)
            leadTimeDiasPromedio = 1;

        if (costoOrdenPedido <= 0)
            costoOrdenPedido = 50.00m;

        return new ProveedorAlimento
        {
            Ruc = ruc.Trim(),
            RazonSocial = razonSocial.Trim(),
            ContactoNombre = contactoNombre?.Trim(),
            Telefono = telefono?.Trim(),
            Email = email?.Trim(),
            LeadTimeDiasPromedio = leadTimeDiasPromedio,
            CostoOrdenPedido = costoOrdenPedido,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    public static ProveedorAlimento Reconstruir(
        int id,
        string ruc,
        string razonSocial,
        string? contactoNombre,
        string? telefono,
        string? email,
        int leadTimeDiasPromedio,
        decimal costoOrdenPedido,
        bool activo,
        DateTime fechaCreacion)
    {
        return new ProveedorAlimento
        {
            Id = id,
            Ruc = ruc,
            RazonSocial = razonSocial,
            ContactoNombre = contactoNombre,
            Telefono = telefono,
            Email = email,
            LeadTimeDiasPromedio = leadTimeDiasPromedio,
            CostoOrdenPedido = costoOrdenPedido,
            Activo = activo,
            FechaCreacion = fechaCreacion
        };
    }
}
