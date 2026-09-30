using ErrorOr;

namespace SierraNevada.Domain.Inventario;

public sealed class LoteAlimento
{
    public int Id { get; private set; }
    public int TipoAlimentoId { get; private set; }
    public int ProveedorId { get; private set; }
    public string CodigoLoteFabrica { get; private set; } = string.Empty;
    public DateOnly? FechaFabricacion { get; private set; }
    public DateOnly FechaVencimiento { get; private set; }
    public decimal PesoPorSacoKg { get; private set; } = 25.00m;
    public int CantidadSacosIngresados { get; private set; }
    public int CantidadSacosActuales { get; private set; }
    public decimal StockKgActual { get; private set; }
    public decimal PrecioUnitarioKg { get; private set; }
    public DateTime FechaRecepcion { get; private set; }
    public bool Activo { get; private set; } = true;

    private LoteAlimento() { }

    public static ErrorOr<LoteAlimento> Crear(
        int tipoAlimentoId,
        int proveedorId,
        string codigoLoteFabrica,
        DateOnly? fechaFabricacion,
        DateOnly fechaVencimiento,
        decimal pesoPorSacoKg,
        int cantidadSacosIngresados,
        decimal precioUnitarioKg)
    {
        if (tipoAlimentoId <= 0)
            return Error.Validation("LoteAlimento.TipoAlimentoId", "Debe especificar un tipo de alimento válido.");

        if (proveedorId <= 0)
            return Error.Validation("LoteAlimento.ProveedorId", "Debe especificar un proveedor válido.");

        if (string.IsNullOrWhiteSpace(codigoLoteFabrica))
            return Error.Validation("LoteAlimento.CodigoLoteFabrica", "El código de lote de fábrica no puede estar vacío.");

        if (pesoPorSacoKg <= 0)
            return Error.Validation("LoteAlimento.PesoPorSacoKg", "El peso por saco debe ser mayor a 0 kg.");

        if (cantidadSacosIngresados <= 0)
            return Error.Validation("LoteAlimento.CantidadSacosIngresados", "La cantidad de sacos ingresados debe ser mayor a 0.");

        if (precioUnitarioKg <= 0)
            return Error.Validation("LoteAlimento.PrecioUnitarioKg", "El precio unitario por kg debe ser mayor a 0.");

        var totalKg = cantidadSacosIngresados * pesoPorSacoKg;

        return new LoteAlimento
        {
            TipoAlimentoId = tipoAlimentoId,
            ProveedorId = proveedorId,
            CodigoLoteFabrica = codigoLoteFabrica.Trim(),
            FechaFabricacion = fechaFabricacion,
            FechaVencimiento = fechaVencimiento,
            PesoPorSacoKg = pesoPorSacoKg,
            CantidadSacosIngresados = cantidadSacosIngresados,
            CantidadSacosActuales = cantidadSacosIngresados,
            StockKgActual = totalKg,
            PrecioUnitarioKg = precioUnitarioKg,
            FechaRecepcion = DateTime.UtcNow,
            Activo = true
        };
    }

    public static LoteAlimento Reconstruir(
        int id,
        int tipoAlimentoId,
        int proveedorId,
        string codigoLoteFabrica,
        DateOnly? fechaFabricacion,
        DateOnly fechaVencimiento,
        decimal pesoPorSacoKg,
        int cantidadSacosIngresados,
        int cantidadSacosActuales,
        decimal stockKgActual,
        decimal precioUnitarioKg,
        DateTime fechaRecepcion,
        bool activo)
    {
        return new LoteAlimento
        {
            Id = id,
            TipoAlimentoId = tipoAlimentoId,
            ProveedorId = proveedorId,
            CodigoLoteFabrica = codigoLoteFabrica,
            FechaFabricacion = fechaFabricacion,
            FechaVencimiento = fechaVencimiento,
            PesoPorSacoKg = pesoPorSacoKg,
            CantidadSacosIngresados = cantidadSacosIngresados,
            CantidadSacosActuales = cantidadSacosActuales,
            StockKgActual = stockKgActual,
            PrecioUnitarioKg = precioUnitarioKg,
            FechaRecepcion = fechaRecepcion,
            Activo = activo
        };
    }

    public ErrorOr<Success> DescontarStock(decimal kgADescontar)
    {
        if (kgADescontar <= 0)
            return Error.Validation("LoteAlimento.DescontarStock", "La cantidad a descontar debe ser mayor a 0 kg.");

        if (StockKgActual < kgADescontar)
            return Error.Validation("LoteAlimento.StockInsuficiente", $"Stock insuficiente en el lote {CodigoLoteFabrica}. Disponible: {StockKgActual:F2} kg, Requerido: {kgADescontar:F2} kg.");

        StockKgActual -= kgADescontar;
        CantidadSacosActuales = (int)Math.Floor(StockKgActual / PesoPorSacoKg);
        if (StockKgActual <= 0)
        {
            StockKgActual = 0;
            CantidadSacosActuales = 0;
        }

        return Result.Success;
    }
}
