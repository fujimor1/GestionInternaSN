namespace SierraNevada.Domain.Inventario;

public enum TipoMovimientoKardex
{
    IngresoCompra,
    EgresoAlimentacion,
    AjusteMerma,
    Devolucion
}

public enum EstadoStockReorden
{
    Critico,    // Stock <= Stock de Seguridad
    Reorden,    // Stock <= Punto de Reorden (ROP)
    Optimo,     // ROP < Stock <= ROP + EOQ
    Sobrestock  // Stock > ROP + EOQ
}
