using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Jaula o Artesa. En el sistema Django anterior eran dos clases separadas heredando
/// de una base abstracta; se consolidan en una sola entidad porque comparten el 100%
/// de los campos y comportamiento (ver docs/diseno-modulo-produccion.md) — más simple
/// de mapear con ADO.NET (una sola tabla, sin herencia tabla-por-tipo).
/// </summary>
public sealed class UnidadProduccion
{
    public int Id { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public TipoUnidadProduccion Tipo { get; private set; }
    public TipoJaula? SubTipoJaula { get; private set; }
    public FormaUnidad Forma { get; private set; }
    public decimal? LargoM { get; private set; }
    public decimal? AnchoM { get; private set; }
    public decimal? DiametroM { get; private set; }
    public decimal AltoM { get; private set; }

    /// <summary>Densidad de siembra objetivo (kg/m³) — semilla FONDEPES 13-15, ver sección 3.1 del doc de investigación.</summary>
    public decimal DensidadSiembraKgM3 { get; private set; }

    private UnidadProduccion() { }

    public static ErrorOr<UnidadProduccion> Crear(
        string codigo,
        TipoUnidadProduccion tipo,
        TipoJaula? subTipoJaula,
        FormaUnidad forma,
        decimal? largoM,
        decimal? anchoM,
        decimal? diametroM,
        decimal altoM,
        decimal densidadSiembraKgM3)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Error.Validation("UnidadProduccion.Codigo", "El código es obligatorio.");

        if (tipo == TipoUnidadProduccion.Artesa && subTipoJaula is not null)
            return Error.Validation("UnidadProduccion.SubTipoJaula", "Una Artesa no tiene sub-tipo de jaula.");

        if (tipo == TipoUnidadProduccion.Jaula && subTipoJaula is null)
            return Error.Validation("UnidadProduccion.SubTipoJaula", "Una Jaula requiere sub-tipo (Juvenil o Engorde).");

        if (altoM <= 0)
            return Error.Validation("UnidadProduccion.AltoM", "La altura/profundidad debe ser mayor a cero.");

        if (densidadSiembraKgM3 <= 0)
            return Error.Validation("UnidadProduccion.DensidadSiembraKgM3", "La densidad de siembra debe ser mayor a cero.");

        return new UnidadProduccion
        {
            Codigo = codigo,
            Tipo = tipo,
            SubTipoJaula = subTipoJaula,
            Forma = forma,
            LargoM = largoM,
            AnchoM = anchoM,
            DiametroM = diametroM,
            AltoM = altoM,
            DensidadSiembraKgM3 = densidadSiembraKgM3,
        };
    }

    public ErrorOr<Success> Actualizar(
        string codigo,
        TipoJaula? subTipoJaula,
        FormaUnidad forma,
        decimal? largoM,
        decimal? anchoM,
        decimal? diametroM,
        decimal altoM,
        decimal densidadSiembraKgM3)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Error.Validation("UnidadProduccion.Codigo", "El código es obligatorio.");

        if (Tipo == TipoUnidadProduccion.Artesa && subTipoJaula is not null)
            return Error.Validation("UnidadProduccion.SubTipoJaula", "Una Artesa no tiene sub-tipo de jaula.");

        if (Tipo == TipoUnidadProduccion.Jaula && subTipoJaula is null)
            return Error.Validation("UnidadProduccion.SubTipoJaula", "Una Jaula requiere sub-tipo (Juvenil o Engorde).");

        if (altoM <= 0)
            return Error.Validation("UnidadProduccion.AltoM", "La altura/profundidad debe ser mayor a cero.");

        if (densidadSiembraKgM3 <= 0)
            return Error.Validation("UnidadProduccion.DensidadSiembraKgM3", "La densidad de siembra debe ser mayor a cero.");

        Codigo = codigo;
        SubTipoJaula = subTipoJaula;
        Forma = forma;
        LargoM = largoM;
        AnchoM = anchoM;
        DiametroM = diametroM;
        AltoM = altoM;
        DensidadSiembraKgM3 = densidadSiembraKgM3;
        return Result.Success;
    }

    public static UnidadProduccion Reconstruir(
        int id, string codigo, TipoUnidadProduccion tipo, TipoJaula? subTipoJaula, FormaUnidad forma,
        decimal? largoM, decimal? anchoM, decimal? diametroM, decimal altoM, decimal densidadSiembraKgM3)
        => new()
        {
            Id = id,
            Codigo = codigo,
            Tipo = tipo,
            SubTipoJaula = subTipoJaula,
            Forma = forma,
            LargoM = largoM,
            AnchoM = anchoM,
            DiametroM = diametroM,
            AltoM = altoM,
            DensidadSiembraKgM3 = densidadSiembraKgM3,
        };

    /// <summary>
    /// Longitud de lado para formas poligonales (hexagonal/decagonal) — misma fórmula que el
    /// sistema anterior. Computada siempre desde DiametroM, nunca guardada — guardarla con
    /// precisión limitada (NUMERIC(10,2)) y reusarla para calcular el volumen introducía un
    /// redondeo en cascada que desviaba la capacidad final unos pocos kg (encontrado 2026-09-23
    /// al comparar contra los valores reales de Django).
    /// </summary>
    public decimal? LadoM
    {
        get
        {
            if (Forma is not (FormaUnidad.Hexagonal or FormaUnidad.Decagonal) || DiametroM is null)
                return null;

            var numLados = Forma == FormaUnidad.Hexagonal ? 6 : 10;
            var apotema = DiametroM.Value / 2;
            return 2 * apotema * (decimal)Math.Tan(Math.PI / numLados);
        }
    }

    /// <summary>Volumen en m³, calculado según la forma — misma lógica que UnidadProduccionBiomasa._calcular_volumen del sistema anterior.</summary>
    public decimal VolumenM3 => Forma switch
    {
        FormaUnidad.Rectangular => (LargoM ?? 0) * (AnchoM ?? 0) * AltoM,
        FormaUnidad.Circular => (decimal)Math.PI * (decimal)Math.Pow((double)((DiametroM ?? 0) / 2), 2) * AltoM,
        FormaUnidad.Hexagonal or FormaUnidad.Decagonal => CalcularVolumenPoligonal(),
        _ => 0m,
    };

    private decimal CalcularVolumenPoligonal()
    {
        var numLados = Forma == FormaUnidad.Hexagonal ? 6 : 10;
        var apotema = (DiametroM ?? 0) / 2;
        var areaBase = (numLados * (LadoM ?? 0) * apotema) / 2;
        return areaBase * AltoM;
    }

    /// <summary>Capacidad máxima de biomasa (kg) según la densidad de siembra semilla.</summary>
    public decimal CapacidadMaximaKg => VolumenM3 * DensidadSiembraKgM3;
}
