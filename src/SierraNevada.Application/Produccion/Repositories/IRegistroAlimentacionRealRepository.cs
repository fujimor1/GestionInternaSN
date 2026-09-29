using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface IRegistroAlimentacionRealRepository
{
    /// <summary>Registros de un lote en un rango de fechas — para calcular FCA real entre dos muestreos.</summary>
    Task<IReadOnlyList<RegistroAlimentacionReal>> ListarPorLoteEnRangoAsync(
        int loteId, DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    /// <summary>Todos los registros de un lote, sin límite de fecha — para reportes agregados.</summary>
    Task<IReadOnlyList<RegistroAlimentacionReal>> ListarPorLoteAsync(int loteId, CancellationToken ct = default);

    Task<int> CrearAsync(RegistroAlimentacionReal registro, CancellationToken ct = default);
}
