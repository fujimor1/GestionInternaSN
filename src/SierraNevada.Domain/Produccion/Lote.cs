using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>
/// El grupo de peces que vive en una unidad de producción específica en un momento dado.
/// Extiende el Lote del sistema Django anterior con CampañaId (siembra repartida en varias
/// unidades desde el inicio) y LotePadreId (selección física que divide un lote a mitad de
/// ciclo) — docs/diseno-modulo-produccion.md, sección 4.2. Los campos de peso/talla "actuales"
/// son una caché de solo lectura que se actualiza vía ActualizarConMuestreo — la serie de
/// tiempo real vive en Muestreo, no aquí (ese era el gap #1 del sistema anterior).
/// </summary>
public sealed class Lote
{
    public int Id { get; private set; }
    public string CodigoLote { get; private set; } = string.Empty;
    public int CampañaId { get; private set; }
    public int? LotePadreId { get; private set; }
    public int? UnidadProduccionId { get; private set; }
    public int? BastidorId { get; private set; }
    public EtapaProductiva EtapaActual { get; private set; }
    public int CantidadInicial { get; private set; }
    public decimal PesoPromedioInicialGr { get; private set; }
    public int CantidadTotalPeces { get; private set; }
    public decimal? TallaPromedioActualCm { get; private set; }
    public decimal? PesoPromedioActualGr { get; private set; }
    public DateOnly FechaIngresoEtapa { get; private set; }
    public DateOnly? FechaFin { get; private set; }
    public bool Activo { get; private set; }

    private Lote() { }

    public static ErrorOr<Lote> Crear(
        string codigoLote,
        int campañaId,
        int cantidadInicial,
        decimal pesoPromedioInicialGr,
        DateOnly fechaIngresoEtapa,
        int? unidadProduccionId = null,
        int? bastidorId = null,
        int? lotePadreId = null,
        decimal? tallaInicialCm = null)
    {
        if (string.IsNullOrWhiteSpace(codigoLote))
            return Error.Validation("Lote.CodigoLote", "El código es obligatorio.");

        if (cantidadInicial <= 0)
            return Error.Validation("Lote.CantidadInicial", "La cantidad inicial debe ser mayor a cero.");

        if (pesoPromedioInicialGr <= 0)
            return Error.Validation("Lote.PesoPromedioInicialGr", "El peso inicial debe ser mayor a cero.");

        if (unidadProduccionId is null && bastidorId is null)
            return Error.Validation("Lote.Ubicacion", "El lote debe estar en una unidad de producción o en un bastidor.");

        // La etapa inicial se deriva de la talla de siembra cuando se conoce (ej. se compraron
        // alevines/juveniles ya crecidos a otro proveedor) — si no se conoce, AlevinajeI por
        // defecto. Bastidor siempre es Ovas. Mismo criterio que Lote.DividirPorSeleccion.
        var etapaInicial = bastidorId is not null
            ? EtapaProductiva.Ovas
            : tallaInicialCm is decimal talla ? EtapaProductivaCalculadora.DesdeTalla(talla) : EtapaProductiva.AlevinajeI;

        return new Lote
        {
            CodigoLote = codigoLote,
            CampañaId = campañaId,
            LotePadreId = lotePadreId,
            UnidadProduccionId = unidadProduccionId,
            BastidorId = bastidorId,
            EtapaActual = etapaInicial,
            CantidadInicial = cantidadInicial,
            PesoPromedioInicialGr = pesoPromedioInicialGr,
            CantidadTotalPeces = cantidadInicial,
            FechaIngresoEtapa = fechaIngresoEtapa,
            Activo = true,
        };
    }

    public static Lote Reconstruir(
        int id, string codigoLote, int campañaId, int? lotePadreId, int? unidadProduccionId, int? bastidorId,
        EtapaProductiva etapaActual, int cantidadInicial, decimal pesoPromedioInicialGr, int cantidadTotalPeces,
        decimal? tallaPromedioActualCm, decimal? pesoPromedioActualGr, DateOnly fechaIngresoEtapa,
        DateOnly? fechaFin, bool activo)
        => new()
        {
            Id = id,
            CodigoLote = codigoLote,
            CampañaId = campañaId,
            LotePadreId = lotePadreId,
            UnidadProduccionId = unidadProduccionId,
            BastidorId = bastidorId,
            EtapaActual = etapaActual,
            CantidadInicial = cantidadInicial,
            PesoPromedioInicialGr = pesoPromedioInicialGr,
            CantidadTotalPeces = cantidadTotalPeces,
            TallaPromedioActualCm = tallaPromedioActualCm,
            PesoPromedioActualGr = pesoPromedioActualGr,
            FechaIngresoEtapa = fechaIngresoEtapa,
            FechaFin = fechaFin,
            Activo = activo,
        };

    public decimal BiomasaActualKg => PesoPromedioActualGr is null
        ? 0m
        : CantidadTotalPeces * PesoPromedioActualGr.Value / 1000m;

    /// <summary>
    /// Actualiza la caché de "estado actual" tras registrar un nuevo Muestreo real. Un muestreo
    /// solo pesa/mide una muestra de peces — NUNCA re-cuenta la población completa — así que no
    /// toca CantidadTotalPeces: esa cifra solo cambia por RegistrarMortalidad/DividirPorSeleccion,
    /// las únicas operaciones que realmente observan cuántos peces hay. Antes el muestreo
    /// sobrescribía el conteo con lo que el operario tipeara, lo que además corrompía en silencio
    /// el FCA real (que se calcula a partir de la biomasa estimada por muestreo).
    /// </summary>
    public ErrorOr<Success> ActualizarConMuestreo(Muestreo muestreo)
    {
        if (muestreo.LoteId != Id)
            return Error.Validation("Lote.Muestreo", "El muestreo no pertenece a este lote.");

        PesoPromedioActualGr = muestreo.PesoPromedioMuestreadoGr;
        TallaPromedioActualCm = muestreo.TallaPromedioMuestreadaCm;
        return Result.Success;
    }

    /// <summary>Descuenta peces muertos tras registrar una RegistroMortalidad real.</summary>
    public ErrorOr<Success> RegistrarMortalidad(int cantidad)
    {
        if (cantidad <= 0)
            return Error.Validation("Lote.Mortalidad", "La cantidad de bajas debe ser mayor a cero.");

        if (cantidad > CantidadTotalPeces)
            return Error.Validation("Lote.Mortalidad", "No puede haber más bajas que peces vivos en el lote.");

        CantidadTotalPeces -= cantidad;
        if (CantidadTotalPeces == 0)
            Activo = false;

        return Result.Success;
    }

    /// <summary>
    /// Selección física por talla (docs/diseno-modulo-produccion.md, sección 5): separa una
    /// porción del lote hacia otra unidad de producción. Descuenta de este lote y crea el lote
    /// hijo, que hereda la misma Campaña y queda enlazado vía LotePadreId.
    /// </summary>
    public ErrorOr<Lote> DividirPorSeleccion(
        string codigoNuevoLote,
        int cantidadAMover,
        int nuevaUnidadProduccionId,
        decimal pesoPromedioGr,
        decimal tallaPromedioCm,
        DateOnly fecha)
    {
        if (cantidadAMover <= 0)
            return Error.Validation("Lote.Seleccion", "La cantidad a mover debe ser mayor a cero.");

        if (cantidadAMover >= CantidadTotalPeces)
            return Error.Validation("Lote.Seleccion", "No puede moverse la totalidad (o más) del lote — use un cambio de etapa/unidad si es todo el lote.");

        var loteHijoResult = Crear(
            codigoNuevoLote, CampañaId, cantidadAMover, pesoPromedioGr, fecha,
            unidadProduccionId: nuevaUnidadProduccionId, lotePadreId: Id);

        if (loteHijoResult.IsError)
            return loteHijoResult.Errors;

        var loteHijo = loteHijoResult.Value;
        // La etapa del hijo se deriva de SU PROPIA talla (los que van a la cabeza pueden ya
        // estar en una etapa más avanzada que el resto del lote padre) — no se hereda sin más.
        loteHijo.EtapaActual = EtapaProductivaCalculadora.DesdeTalla(tallaPromedioCm);
        loteHijo.TallaPromedioActualCm = tallaPromedioCm;
        loteHijo.PesoPromedioActualGr = pesoPromedioGr;

        CantidadTotalPeces -= cantidadAMover;

        return loteHijo;
    }

    /// <summary>Avanza el lote a la siguiente etapa productiva. El registro de HistorialMovimiento lo arma Application.</summary>
    public ErrorOr<Success> CambiarEtapa(EtapaProductiva nuevaEtapa, DateOnly fecha)
    {
        if (nuevaEtapa <= EtapaActual)
            return Error.Validation("Lote.EtapaActual", "La nueva etapa debe ser posterior a la etapa actual.");

        EtapaActual = nuevaEtapa;
        FechaIngresoEtapa = fecha;
        return Result.Success;
    }

    public ErrorOr<Success> Finalizar(DateOnly fecha)
    {
        if (!Activo)
            return Error.Validation("Lote.Activo", "El lote ya está finalizado.");

        Activo = false;
        FechaFin = fecha;
        return Result.Success;
    }
}
