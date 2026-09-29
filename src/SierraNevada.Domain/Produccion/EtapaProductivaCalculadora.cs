namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Deriva la etapa productiva esperada a partir de la talla, usando las bandas del protocolo
/// FONDEPES (docs/investigacion-parametros-produccion.md sección 3.2 — las mismas que definen
/// EtapaProductiva; comparadas contra Excel y Django en la sección 11.2 de arquitectura-tecnica.md,
/// FONDEPES es la fuente elegida por ser la oficial y no tener huecos). Se usa para:
/// (1) asignar la etapa real de un lote hijo creado por Selección según su propia talla, no la
/// del lote padre; (2) validar compatibilidad de talla al mover peces a otra unidad.
/// </summary>
public static class EtapaProductivaCalculadora
{
    public static EtapaProductiva DesdeTalla(decimal tallaCm) => tallaCm switch
    {
        < 5.0m => EtapaProductiva.AlevinajeI,
        < 8.0m => EtapaProductiva.AlevinajeII,
        < 12.0m => EtapaProductiva.AlevinajeIII,
        < 14.0m => EtapaProductiva.JuvenilesI,
        < 17.0m => EtapaProductiva.JuvenilesII,
        < 20.0m => EtapaProductiva.EngordeI,
        _ => EtapaProductiva.EngordeII,
    };

    /// <summary>
    /// El tipo de unidad físicamente correcto para una etapa — igual al patrón del sistema
    /// anterior (Artesa solo para Alevinaje, Jaula Juvenil solo para Juveniles, Jaula Engorde
    /// solo para Engorde). El SubTipoJaula de una unidad es un campo editable, no una clasificación
    /// permanente — puede reasignarse entre campañas, pero en un momento dado debe corresponder.
    /// </summary>
    public static bool TipoUnidadCorresponde(UnidadProduccion unidad, EtapaProductiva etapa) => etapa switch
    {
        EtapaProductiva.AlevinajeI or EtapaProductiva.AlevinajeII or EtapaProductiva.AlevinajeIII =>
            unidad.Tipo == TipoUnidadProduccion.Artesa,
        EtapaProductiva.JuvenilesI or EtapaProductiva.JuvenilesII =>
            unidad.Tipo == TipoUnidadProduccion.Jaula && unidad.SubTipoJaula == TipoJaula.Juvenil,
        EtapaProductiva.EngordeI or EtapaProductiva.EngordeII =>
            unidad.Tipo == TipoUnidadProduccion.Jaula && unidad.SubTipoJaula == TipoJaula.Engorde,
        _ => false,
    };

    /// <summary>
    /// Duración esperada (días) según el protocolo FONDEPES, Tabla 5
    /// (docs/investigacion-parametros-produccion.md sección 3.2) — cita directa, no derivada.
    /// Null para Ovas (el protocolo no da una duración de incubación en esta tabla).
    /// </summary>
    public static (int MinDias, int MaxDias)? DuracionEsperadaFondepes(EtapaProductiva etapa) => etapa switch
    {
        EtapaProductiva.AlevinajeI => (30, 45),
        EtapaProductiva.AlevinajeII => (30, 30),
        EtapaProductiva.AlevinajeIII => (30, 30),
        EtapaProductiva.JuvenilesI => (30, 30),
        EtapaProductiva.JuvenilesII => (30, 30),
        EtapaProductiva.EngordeI => (60, 60),
        EtapaProductiva.EngordeII => (60, 90),
        _ => null,
    };
}

/// <summary>
/// Los 6 tramos de dieta reales de Sierra Nevada (columna M del Excel, marco "SierraNevada" —
/// ver docs/investigacion-parametros-produccion.md sección 4.2). Independiente de EtapaProductiva:
/// se deriva solo de la talla, igual que hacen las barras de progreso del frontend
/// (frontend/src/produccion/etapaObjetivos.ts) — para que backend y frontend no se desincronicen.
/// </summary>
public static class BandaSierraNevada
{
    public static string Para(decimal tallaCm) => tallaCm switch
    {
        <= 4m => "Pre inicio",
        <= 7.07m => "Inicio",
        <= 11.82m => "Crecimiento I",
        <= 18.9m => "Crecimiento II",
        <= 23.63m => "Acabado simple",
        <= 30m => "Acabado pigmento",
        _ => "Reproductores",
    };

    /// <summary>
    /// Duración esperada (días) por tramo, NO es un dato que el Excel declare directamente en
    /// ninguna celda — se obtuvo simulando día a día el propio modelo del Excel real (fórmulas
    /// exactas de las columnas D-M de la hoja "campania 1": ración, FCA objetivo, ganancia diaria),
    /// arrancando en talla 3.5cm con K=1.123 fijo (igual que asume el Excel). Los 2 huecos reales
    /// de la columna G (ración) se interpolaron solo para que la simulación no se quedara trabada
    /// con ganancia cero — la tabla de referencia real (marco SierraNevada ya cargado en el
    /// sistema) sigue mostrando esos huecos sin rellenar, esto es un cálculo aparte.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> DiasEsperados = new Dictionary<string, int>
    {
        ["Pre inicio"] = 7,
        ["Inicio"] = 39,
        ["Crecimiento I"] = 55,
        ["Crecimiento II"] = 68,
        ["Acabado simple"] = 45,
        ["Acabado pigmento"] = 24,
    };
}
