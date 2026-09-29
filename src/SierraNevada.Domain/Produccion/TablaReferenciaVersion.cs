using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Reemplaza las tablas de ración/FCA/mortalidad hardcodeadas en el sistema Django anterior
/// (gap #3, docs/diseno-modulo-produccion.md sección 4.6). Cada recalibración crea una nueva
/// versión con su fuente ("FONDEPES semilla 2026-09" vs "Calibrado 2027-03 con 12 lotes") —
/// no se pierde ni se sobrescribe la anterior. Es el mecanismo que hace auditable la evolución
/// del sistema hacia la fase 2 (motor de calibración) y, más adelante, la fase 3 (ML).
/// </summary>
public sealed class TablaReferenciaVersion
{
    private readonly List<TablaReferenciaValor> _valores = [];

    public int Id { get; private set; }
    public TipoTablaReferencia Tipo { get; private set; }
    public MarcoReferencia Marco { get; private set; }
    public DateOnly FechaVigenciaDesde { get; private set; }
    public string Fuente { get; private set; } = string.Empty;
    public bool Activa { get; private set; }
    public IReadOnlyList<TablaReferenciaValor> Valores => _valores.AsReadOnly();

    private TablaReferenciaVersion() { }

    public static ErrorOr<TablaReferenciaVersion> Crear(
        TipoTablaReferencia tipo, MarcoReferencia marco, DateOnly fechaVigenciaDesde, string fuente)
    {
        if (string.IsNullOrWhiteSpace(fuente))
            return Error.Validation("TablaReferenciaVersion.Fuente", "La fuente es obligatoria (para trazabilidad de la calibración).");

        return new TablaReferenciaVersion
        {
            Tipo = tipo,
            Marco = marco,
            FechaVigenciaDesde = fechaVigenciaDesde,
            Fuente = fuente,
            Activa = false,
        };
    }

    public static TablaReferenciaVersion Reconstruir(
        int id, TipoTablaReferencia tipo, MarcoReferencia marco, DateOnly fechaVigenciaDesde, string fuente, bool activa,
        IEnumerable<TablaReferenciaValor> valores)
    {
        var version = new TablaReferenciaVersion
        {
            Id = id, Tipo = tipo, Marco = marco, FechaVigenciaDesde = fechaVigenciaDesde, Fuente = fuente, Activa = activa,
        };
        version._valores.AddRange(valores);
        return version;
    }

    public ErrorOr<Success> AgregarValor(
        decimal tallaMinCm, decimal tallaMaxCm, decimal valor, decimal? temperaturaMinC = null, decimal? temperaturaMaxC = null)
    {
        if (tallaMaxCm <= tallaMinCm)
            return Error.Validation("TablaReferenciaValor.Talla", "La talla máxima debe ser mayor a la mínima.");

        if (temperaturaMaxC is not null && temperaturaMinC is not null && temperaturaMaxC <= temperaturaMinC)
            return Error.Validation("TablaReferenciaValor.Temperatura", "La temperatura máxima debe ser mayor a la mínima.");

        _valores.Add(TablaReferenciaValor.Reconstruir(0, Id, tallaMinCm, tallaMaxCm, temperaturaMinC, temperaturaMaxC, valor));
        return Result.Success;
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;
}

/// <summary>Un renglón de la tabla — por rango de talla, opcionalmente también por rango de temperatura (Klontz 1991).</summary>
public sealed class TablaReferenciaValor
{
    public int Id { get; private set; }
    public int VersionId { get; private set; }
    public decimal TallaMinCm { get; private set; }
    public decimal TallaMaxCm { get; private set; }
    public decimal? TemperaturaMinC { get; private set; }
    public decimal? TemperaturaMaxC { get; private set; }
    public decimal Valor { get; private set; }

    private TablaReferenciaValor() { }

    public static TablaReferenciaValor Reconstruir(
        int id, int versionId, decimal tallaMinCm, decimal tallaMaxCm,
        decimal? temperaturaMinC, decimal? temperaturaMaxC, decimal valor)
        => new()
        {
            Id = id,
            VersionId = versionId,
            TallaMinCm = tallaMinCm,
            TallaMaxCm = tallaMaxCm,
            TemperaturaMinC = temperaturaMinC,
            TemperaturaMaxC = temperaturaMaxC,
            Valor = valor,
        };

    public bool AplicaATalla(decimal tallaCm) => tallaCm >= TallaMinCm && tallaCm < TallaMaxCm;

    public bool AplicaATemperatura(decimal temperaturaC) =>
        TemperaturaMinC is null || TemperaturaMaxC is null
        || (temperaturaC >= TemperaturaMinC && temperaturaC < TemperaturaMaxC);
}
