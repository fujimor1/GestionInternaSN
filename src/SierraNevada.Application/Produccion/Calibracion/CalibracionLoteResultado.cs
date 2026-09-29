using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Calibracion;

/// <summary>Un punto de la serie de tiempo de Factor de Condición (K) — de los Muestreo reales del lote.</summary>
public sealed record PuntoFactorCondicion(DateOnly Fecha, decimal TallaCm, decimal PesoGr, decimal FactorK);

/// <summary>
/// Duración real de una etapa — null en FechaFin/DiasReales significa "en curso, todavía no
/// terminó". DiasEsperadosMin/Max viene directo del protocolo FONDEPES (Tabla 5), null para Ovas.
/// </summary>
public sealed record DuracionEtapaReal(
    EtapaProductiva Etapa, DateOnly FechaInicio, DateOnly? FechaFin, int? DiasReales,
    int? DiasEsperadosMinFondepes, int? DiasEsperadosMaxFondepes);

/// <summary>
/// Duración real por tramo de dieta de Sierra Nevada (columna M del Excel — Pre inicio, Inicio,
/// Crecimiento I/II, Acabado simple/pigmento), derivada de la TALLA de cada muestreo, no de un
/// evento explícito de cambio de etapa (Sierra Nevada no tiene esa acción en el sistema).
/// DiasEsperados sale de simular el propio modelo del Excel día a día (ver BandaSierraNevada).
/// </summary>
public sealed record DuracionBandaSierraNevada(
    string Tramo, DateOnly FechaInicio, DateOnly? FechaFin, int? DiasReales, int? DiasEsperados);

/// <summary>FCA real entre dos muestreos consecutivos: alimento real acumulado / ganancia de biomasa real en ese período.</summary>
public sealed record FcaRealPeriodo(DateOnly Desde, DateOnly Hasta, decimal AlimentoAcumuladoKg, decimal GananciaBiomasaKg, decimal? FcaReal);

/// <summary>
/// Ración real entregada entre dos muestreos consecutivos (% de biomasa/día, tomando la biomasa
/// del muestreo inicial — misma convención que usa el Excel real de la empresa, columna H) vs. lo
/// recomendado por CADA marco de referencia activo para la talla de ese momento — FONDEPES
/// (objetivo/buena gestión) y SierraNevada (cómo opera realmente hoy la empresa, según su propio
/// Excel), uno al lado del otro, no uno reemplazando al otro. AlimentoAcumuladoKg es el TOTAL del
/// período completo (puede abarcar varios días) — AlimentoPromedioDiaKg es el que realmente se
/// compara contra el Excel (que reporta consumo día a día, no por período).
/// </summary>
public sealed record RacionRealPeriodo(
    DateOnly Desde, DateOnly Hasta, decimal TallaInicioCm, decimal PesoInicioGr, decimal AlimentoAcumuladoKg,
    decimal AlimentoPromedioDiaKg, decimal BiomasaInicioKg, decimal PorcentajeRealPorDia,
    decimal? PorcentajeRecomendadoFondepes, decimal? PorcentajeRecomendadoSierraNevada);

/// <summary>
/// Cuánto dar de comer HOY, no un promedio de hace una o dos semanas: usa el peso/talla del
/// último muestreo (quincenal, por eso puede tener unos días de atraso) pero la cantidad de peces
/// vivos ACTUAL (las bajas se cuentan y descuentan a diario, no esperan al próximo muestreo). Null
/// si el lote todavía no tiene ningún muestreo registrado.
/// </summary>
public sealed record RacionRecomendadaHoy(
    DateOnly FechaUltimoMuestreo, decimal TallaUltimoMuestreoCm, decimal PesoUltimoMuestreoGr,
    int PecesVivosHoy, decimal BiomasaHoyKg,
    decimal? PorcentajeRecomendadoFondepes, decimal? AlimentoRecomendadoHoyKgFondepes,
    decimal? PorcentajeRecomendadoSierraNevada, decimal? AlimentoRecomendadoHoyKgSierraNevada);

/// <summary>
/// Talla y peso en función del TIEMPO — no "en qué rango estoy", sino "cómo debería ir creciendo
/// día a día" comparado con cómo creció de verdad. RealDesdePrimerMuestreo son los muestreos
/// reales del lote, con Dia=0 en el primero. Cada Esperado* está realineado (RebasarCurva) para
/// que su Dia=0 caiga en la talla de partida REAL del lote — no en 3.5cm siempre — así se compara
/// "desde donde arrancó este lote, cómo debería haber seguido" contra lo que pasó de verdad.
/// </summary>
public sealed record CurvaCrecimientoResultado(
    IReadOnlyList<PuntoCurvaCrecimiento> EsperadoFondepes,
    IReadOnlyList<PuntoCurvaCrecimiento> EsperadoSierraNevada,
    IReadOnlyList<PuntoCurvaCrecimiento> Real);

public sealed record MortalidadRealResumen(int TotalBajas, int CantidadInicial, decimal PorcentajeAcumulado);

/// <summary>Densidad actual del lote vs. la referencia activa (semilla FONDEPES o recalibrada).</summary>
public sealed record DensidadActual(
    decimal BiomasaActualKg, decimal? VolumenM3, decimal? DensidadKgM3,
    decimal? DensidadReferenciaMaxKgM3, bool? SuperaReferencia);

/// <summary>
/// Resultado completo de la calibración de un lote: todo lo real (K, duración por etapa, FCA,
/// mortalidad, densidad), listo para compararse contra las tablas de referencia semilla/calibradas.
/// Es el corazón de la fase 2 (docs/investigacion-parametros-produccion.md sección 8.2).
/// </summary>
public sealed record CalibracionLoteResultado(
    int LoteId,
    string CodigoLote,
    IReadOnlyList<PuntoFactorCondicion> SerieFactorCondicion,
    IReadOnlyList<DuracionEtapaReal> DuracionesPorEtapa,
    IReadOnlyList<DuracionBandaSierraNevada> DuracionesSierraNevada,
    IReadOnlyList<FcaRealPeriodo> FcaPorPeriodo,
    IReadOnlyList<RacionRealPeriodo> RacionPorPeriodo,
    RacionRecomendadaHoy? RacionHoy,
    CurvaCrecimientoResultado CurvaCrecimiento,
    MortalidadRealResumen Mortalidad,
    DensidadActual Densidad);
