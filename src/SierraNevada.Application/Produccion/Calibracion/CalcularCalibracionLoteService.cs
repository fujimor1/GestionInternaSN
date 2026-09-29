using ErrorOr;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Calibracion;

/// <summary>
/// El motor de calibración (fase 2): calcula K real, duración real por etapa, FCA real,
/// mortalidad real y densidad real de un lote a partir de sus datos capturados (Muestreo,
/// RegistroAlimentacionReal, RegistroMortalidad, HistorialMovimiento) — para compararlos contra
/// las tablas de referencia semilla (FONDEPES/Excel) y, con el tiempo, recalibrarlas.
/// Ver docs/investigacion-parametros-produccion.md sección 8.2 (fase 2 de la arquitectura de 3 fases).
/// </summary>
public sealed class CalcularCalibracionLoteService(
    ILoteRepository loteRepository,
    IMuestreoRepository muestreoRepository,
    IRegistroAlimentacionRealRepository alimentacionRepository,
    IRegistroMortalidadRepository mortalidadRepository,
    IHistorialMovimientoRepository historialRepository,
    IUnidadProduccionRepository unidadProduccionRepository,
    ITablaReferenciaVersionRepository tablaReferenciaRepository)
{
    public async Task<ErrorOr<CalibracionLoteResultado>> CalcularAsync(int loteId, CancellationToken ct = default)
    {
        var lote = await loteRepository.ObtenerPorIdAsync(loteId, ct);
        if (lote is null)
            return Error.NotFound("Lote.NoEncontrado", "El lote no existe.");

        var muestreos = await muestreoRepository.ListarPorLoteAsync(loteId, ct);

        // El K y la curva de crecimiento cuentan la historia completa del linaje (Ovas -> ... ->
        // aquí), no solo lo que pasó en esta unidad física — cada Selección crea un lote nuevo con
        // su propio historial, así que sin esto la curva de un lote en Engorde saltaría directo a
        // esas pocas semanas, sin mostrar cómo llegó hasta ahí.
        var muestreosLineage = await ObtenerMuestreosLineageAsync(lote, ct);

        var serieFactorCondicion = muestreosLineage
            .Select(m => new PuntoFactorCondicion(m.Fecha, m.TallaPromedioMuestreadaCm, m.PesoPromedioMuestreadoGr, m.FactorCondicionK))
            .ToList();

        var duracionesPorEtapa = await CalcularDuracionesPorEtapaAsync(loteId, ct);
        var duracionesSierraNevada = CalcularDuracionesSierraNevada(muestreos);
        var fcaPorPeriodo = await CalcularFcaPorPeriodoAsync(loteId, muestreos, ct);

        var tablaFondepes = await tablaReferenciaRepository.ObtenerActivaAsync(TipoTablaReferencia.Racion, MarcoReferencia.Fondepes, ct);
        var tablaSierraNevada = await tablaReferenciaRepository.ObtenerActivaAsync(TipoTablaReferencia.Racion, MarcoReferencia.SierraNevada, ct);
        var alimentacionParaRacion = muestreos.Count >= 2
            ? await alimentacionRepository.ListarPorLoteEnRangoAsync(loteId, muestreos[0].Fecha, muestreos[^1].Fecha, ct)
            : [];
        var racionPorPeriodo = CalcularRacionPorPeriodo(muestreos, alimentacionParaRacion, tablaFondepes, tablaSierraNevada);
        var racionHoy = CalcularRacionRecomendadaHoy(lote, muestreos, tablaFondepes, tablaSierraNevada);

        var mortalidad = await CalcularMortalidadAsync(loteId, lote.CantidadInicial, ct);
        var densidad = await CalcularDensidadActualAsync(lote, ct);
        var curvaCrecimiento = CalcularCurvaCrecimiento(muestreosLineage);

        return new CalibracionLoteResultado(
            loteId, lote.CodigoLote, serieFactorCondicion, duracionesPorEtapa, duracionesSierraNevada, fcaPorPeriodo,
            racionPorPeriodo, racionHoy, curvaCrecimiento, mortalidad, densidad);
    }

    /// <summary>
    /// Todos los muestreos del lote actual MÁS los de todos sus ancestros (lote_padre_id hacia
    /// arriba hasta la raíz de la campaña), ordenados por fecha — para que el K y la curva de
    /// crecimiento cuenten la historia completa, no solo desde la última Selección.
    /// </summary>
    private async Task<IReadOnlyList<Muestreo>> ObtenerMuestreosLineageAsync(Lote lote, CancellationToken ct)
    {
        var resultado = new List<Muestreo>(await muestreoRepository.ListarPorLoteAsync(lote.Id, ct));

        var actual = lote;
        while (actual.LotePadreId is int padreId)
        {
            var padre = await loteRepository.ObtenerPorIdAsync(padreId, ct);
            if (padre is null)
                break;

            resultado.AddRange(await muestreoRepository.ListarPorLoteAsync(padre.Id, ct));
            actual = padre;
        }

        return resultado.OrderBy(m => m.Fecha).ToList();
    }

    private async Task<IReadOnlyList<DuracionEtapaReal>> CalcularDuracionesPorEtapaAsync(int loteId, CancellationToken ct)
    {
        var eventos = await historialRepository.ListarEventosEtapaPorLoteAsync(loteId, ct);
        var resultado = new List<DuracionEtapaReal>();

        for (var i = 0; i < eventos.Count; i++)
        {
            var etapa = eventos[i].EtapaNueva!.Value;
            var fechaInicio = DateOnly.FromDateTime(eventos[i].Fecha);
            DateOnly? fechaFin = i + 1 < eventos.Count ? DateOnly.FromDateTime(eventos[i + 1].Fecha) : null;
            var diasReales = fechaFin.HasValue ? fechaFin.Value.DayNumber - fechaInicio.DayNumber : (int?)null;
            var esperado = EtapaProductivaCalculadora.DuracionEsperadaFondepes(etapa);

            resultado.Add(new DuracionEtapaReal(etapa, fechaInicio, fechaFin, diasReales, esperado?.MinDias, esperado?.MaxDias));
        }

        return resultado;
    }

    /// <summary>
    /// Duración real por tramo de dieta de Sierra Nevada — a diferencia de DuracionesPorEtapa, no
    /// hay una acción explícita de "cambiar de sub-etapa" en el sistema (Sierra Nevada no la tiene
    /// como concepto operativo), así que se deriva de la TALLA de cada muestreo consecutivo: cuando
    /// el tramo cambia de un muestreo al siguiente, ahí se marca la transición.
    /// </summary>
    private static IReadOnlyList<DuracionBandaSierraNevada> CalcularDuracionesSierraNevada(IReadOnlyList<Muestreo> muestreos)
    {
        if (muestreos.Count == 0)
            return [];

        var resultado = new List<DuracionBandaSierraNevada>();
        string? tramoActual = null;
        var inicioTramo = default(DateOnly);

        foreach (var m in muestreos)
        {
            var tramo = BandaSierraNevada.Para(m.TallaPromedioMuestreadaCm);
            if (tramo != tramoActual)
            {
                if (tramoActual is not null)
                {
                    var dias = m.Fecha.DayNumber - inicioTramo.DayNumber;
                    resultado.Add(new DuracionBandaSierraNevada(
                        tramoActual, inicioTramo, m.Fecha, dias, BandaSierraNevada.DiasEsperados.GetValueOrDefault(tramoActual)));
                }

                tramoActual = tramo;
                inicioTramo = m.Fecha;
            }
        }

        resultado.Add(new DuracionBandaSierraNevada(
            tramoActual!, inicioTramo, null, null, BandaSierraNevada.DiasEsperados.GetValueOrDefault(tramoActual!)));

        return resultado;
    }

    private async Task<IReadOnlyList<FcaRealPeriodo>> CalcularFcaPorPeriodoAsync(
        int loteId, IReadOnlyList<Muestreo> muestreos, CancellationToken ct)
    {
        if (muestreos.Count < 2)
            return [];

        // Un solo viaje a la base para todo el rango de alimentación, en vez de una consulta por período.
        var alimentacion = await alimentacionRepository.ListarPorLoteEnRangoAsync(
            loteId, muestreos[0].Fecha, muestreos[^1].Fecha, ct);

        var resultado = new List<FcaRealPeriodo>();

        for (var i = 0; i < muestreos.Count - 1; i++)
        {
            var actual = muestreos[i];
            var siguiente = muestreos[i + 1];

            var alimentoAcumuladoKg = alimentacion
                .Where(a => a.Fecha > actual.Fecha && a.Fecha <= siguiente.Fecha)
                .Sum(a => a.CantidadKgEntregada);

            var gananciaBiomasaKg = siguiente.BiomasaEstimadaKg - actual.BiomasaEstimadaKg;
            var fcaReal = gananciaBiomasaKg > 0 ? alimentoAcumuladoKg / gananciaBiomasaKg : (decimal?)null;

            resultado.Add(new FcaRealPeriodo(actual.Fecha, siguiente.Fecha, alimentoAcumuladoKg, gananciaBiomasaKg, fcaReal));
        }

        return resultado;
    }

    /// <summary>
    /// Ración real (%biomasa/día, tomando la biomasa del muestreo inicial del período — misma
    /// convención que usa el Excel real de la empresa, columna H "BIOMASA(ayer) × G/100") vs. la
    /// recomendada por la tabla de referencia activa para la talla de ese momento. Es un promedio
    /// RETROSPECTIVO por período (puede abarcar 1-2 semanas) — no cuánto dar HOY (eso lo resuelve
    /// CalcularRacionRecomendadaHoy, con la cantidad de peces actual, no la de hace dos semanas).
    /// </summary>
    private static IReadOnlyList<RacionRealPeriodo> CalcularRacionPorPeriodo(
        IReadOnlyList<Muestreo> muestreos, IReadOnlyList<RegistroAlimentacionReal> alimentacion,
        TablaReferenciaVersion? tablaFondepes, TablaReferenciaVersion? tablaSierraNevada)
    {
        if (muestreos.Count < 2)
            return [];

        var resultado = new List<RacionRealPeriodo>();

        for (var i = 0; i < muestreos.Count - 1; i++)
        {
            var actual = muestreos[i];
            var siguiente = muestreos[i + 1];
            var dias = siguiente.Fecha.DayNumber - actual.Fecha.DayNumber;
            if (dias <= 0)
                continue;

            var alimentoAcumuladoKg = alimentacion
                .Where(a => a.Fecha > actual.Fecha && a.Fecha <= siguiente.Fecha)
                .Sum(a => a.CantidadKgEntregada);

            var biomasaInicioKg = actual.BiomasaEstimadaKg;
            var alimentoPromedioDiaKg = alimentoAcumuladoKg / dias;
            var porcentajeRealPorDia = biomasaInicioKg > 0 ? alimentoPromedioDiaKg / biomasaInicioKg * 100m : 0m;
            var porcentajeRecomendadoFondepes = tablaFondepes?.Valores.FirstOrDefault(v => v.AplicaATalla(actual.TallaPromedioMuestreadaCm))?.Valor;
            var porcentajeRecomendadoSierraNevada = tablaSierraNevada?.Valores.FirstOrDefault(v => v.AplicaATalla(actual.TallaPromedioMuestreadaCm))?.Valor;

            resultado.Add(new RacionRealPeriodo(
                actual.Fecha, siguiente.Fecha, actual.TallaPromedioMuestreadaCm, actual.PesoPromedioMuestreadoGr,
                alimentoAcumuladoKg, alimentoPromedioDiaKg, biomasaInicioKg, porcentajeRealPorDia,
                porcentajeRecomendadoFondepes, porcentajeRecomendadoSierraNevada));
        }

        return resultado;
    }

    /// <summary>
    /// Cuánto dar de comer HOY: usa el peso/talla del ÚLTIMO muestreo (quincenal, sobre una
    /// muestra chica — por eso puede tener unos días de atraso) pero la cantidad de peces vivos
    /// ACTUAL del lote, que sí se mantiene al día porque las bajas se cuentan y registran a diario
    /// (Lote.CantidadTotalPeces baja en el momento en que se registra cada mortalidad, no espera
    /// al próximo muestreo). Sin esto, el operario solo tenía el promedio retrospectivo de la
    /// última semana o dos — no una cifra para dar de comer hoy mismo, actualizada automáticamente
    /// según las bajas que ya se contaron.
    /// </summary>
    private static RacionRecomendadaHoy? CalcularRacionRecomendadaHoy(
        Lote lote, IReadOnlyList<Muestreo> muestreos, TablaReferenciaVersion? tablaFondepes, TablaReferenciaVersion? tablaSierraNevada)
    {
        if (muestreos.Count == 0)
            return null;

        var ultimo = muestreos[^1];
        var biomasaHoyKg = lote.CantidadTotalPeces * ultimo.PesoPromedioMuestreadoGr / 1000m;

        var pctFondepes = tablaFondepes?.Valores.FirstOrDefault(v => v.AplicaATalla(ultimo.TallaPromedioMuestreadaCm))?.Valor;
        var pctSierraNevada = tablaSierraNevada?.Valores.FirstOrDefault(v => v.AplicaATalla(ultimo.TallaPromedioMuestreadaCm))?.Valor;

        return new RacionRecomendadaHoy(
            ultimo.Fecha, ultimo.TallaPromedioMuestreadaCm, ultimo.PesoPromedioMuestreadoGr,
            lote.CantidadTotalPeces, biomasaHoyKg,
            pctFondepes, pctFondepes is decimal pf ? biomasaHoyKg * pf / 100m : null,
            pctSierraNevada, pctSierraNevada is decimal ps ? biomasaHoyKg * ps / 100m : null);
    }

    /// <summary>
    /// Talla/peso esperados en función del tiempo, realineados al punto de partida REAL del lote
    /// (no siempre 3.5cm — un lote nacido de una Selección puede arrancar en cualquier talla).
    /// Cada marco usa su propia herramienta (ver CurvaCrecimientoReferencia): FONDEPES interpola
    /// semanalmente entre los rangos del protocolo, Sierra Nevada simula día a día la fórmula real
    /// del Excel. No tienen por qué coincidir en granularidad ni en trayectoria — son marcos distintos.
    /// </summary>
    private static CurvaCrecimientoResultado CalcularCurvaCrecimiento(IReadOnlyList<Muestreo> muestreos)
    {
        if (muestreos.Count == 0)
            return new CurvaCrecimientoResultado([], [], []);

        var tallaInicial = muestreos[0].TallaPromedioMuestreadaCm;
        var fechaInicial = muestreos[0].Fecha;

        var real = muestreos
            .Select(m => new PuntoCurvaCrecimiento(m.Fecha.DayNumber - fechaInicial.DayNumber, m.TallaPromedioMuestreadaCm, m.PesoPromedioMuestreadoGr))
            .ToList();

        var esperadoFondepes = RebasarCurva(CurvaCrecimientoReferencia.GenerarFondepesSemanal(), tallaInicial);
        var esperadoSierraNevada = RebasarCurva(CurvaCrecimientoReferencia.GenerarSierraNevadaDiaria(), tallaInicial);

        return new CurvaCrecimientoResultado(esperadoFondepes, esperadoSierraNevada, real);
    }

    /// <summary>Desplaza una curva de referencia para que su Día=0 caiga en el punto más cercano a la talla de partida real.</summary>
    private static IReadOnlyList<PuntoCurvaCrecimiento> RebasarCurva(IReadOnlyList<PuntoCurvaCrecimiento> curva, decimal tallaInicial)
    {
        if (curva.Count == 0)
            return [];

        var basePunto = curva.OrderBy(p => Math.Abs(p.TallaCm - tallaInicial)).First();

        return curva
            .Where(p => p.Dia >= basePunto.Dia)
            .Select(p => new PuntoCurvaCrecimiento(p.Dia - basePunto.Dia, p.TallaCm, p.PesoGr))
            .ToList();
    }

    private async Task<MortalidadRealResumen> CalcularMortalidadAsync(int loteId, int cantidadInicial, CancellationToken ct)
    {
        var registros = await mortalidadRepository.ListarPorLoteAsync(loteId, ct);
        var totalBajas = registros.Sum(r => r.Cantidad);
        var porcentaje = cantidadInicial > 0 ? 100m * totalBajas / cantidadInicial : 0m;

        return new MortalidadRealResumen(totalBajas, cantidadInicial, porcentaje);
    }

    private async Task<DensidadActual> CalcularDensidadActualAsync(Lote lote, CancellationToken ct)
    {
        var biomasaActualKg = lote.BiomasaActualKg;

        decimal? volumenM3 = null;
        if (lote.UnidadProduccionId is int unidadId)
        {
            var unidad = await unidadProduccionRepository.ObtenerPorIdAsync(unidadId, ct);
            volumenM3 = unidad?.VolumenM3;
        }

        var densidadKgM3 = volumenM3 is > 0 ? biomasaActualKg / volumenM3.Value : (decimal?)null;

        decimal? densidadReferenciaMaxKgM3 = null;
        if (lote.TallaPromedioActualCm is decimal talla)
        {
            var tablaDensidad = await tablaReferenciaRepository.ObtenerActivaAsync(TipoTablaReferencia.Densidad, MarcoReferencia.Fondepes, ct);
            densidadReferenciaMaxKgM3 = tablaDensidad?.Valores.FirstOrDefault(v => v.AplicaATalla(talla))?.Valor;
        }

        var superaReferencia = densidadKgM3.HasValue && densidadReferenciaMaxKgM3.HasValue
            ? densidadKgM3.Value > densidadReferenciaMaxKgM3.Value
            : (bool?)null;

        return new DensidadActual(biomasaActualKg, volumenM3, densidadKgM3, densidadReferenciaMaxKgM3, superaReferencia);
    }
}
