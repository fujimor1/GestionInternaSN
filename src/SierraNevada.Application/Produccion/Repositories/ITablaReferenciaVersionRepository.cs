using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Repositories;

public interface ITablaReferenciaVersionRepository
{
    /// <summary>La versión activa de un tipo de tabla + marco (ración/FCA/mortalidad/densidad × FONDEPES/SierraNevada), con sus valores cargados.</summary>
    Task<TablaReferenciaVersion?> ObtenerActivaAsync(TipoTablaReferencia tipo, MarcoReferencia marco, CancellationToken ct = default);

    Task<TablaReferenciaVersion?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>Historial de versiones de un tipo + marco — para auditar cómo evolucionó la calibración.</summary>
    Task<IReadOnlyList<TablaReferenciaVersion>> ListarPorTipoAsync(TipoTablaReferencia tipo, MarcoReferencia marco, CancellationToken ct = default);

    /// <summary>Crea la versión + sus valores, y la marca activa (desactivando la anterior) en una sola transacción.</summary>
    Task<int> CrearYActivarAsync(TablaReferenciaVersion version, CancellationToken ct = default);
}
