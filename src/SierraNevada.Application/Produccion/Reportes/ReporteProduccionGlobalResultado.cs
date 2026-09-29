using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Reportes;

/// <summary>
/// Cuánto se dio de comer y cuánta mortalidad hubo mientras los lotes pasaban por esta etapa
/// (marco FONDEPES), agregado sobre TODOS los lotes activos del sistema — no uno solo. Los días
/// se agregan solo sobre los tramos ya terminados (FechaFin no nula) de cada lote.
/// </summary>
public sealed record ResumenEtapaGlobal(
    EtapaProductiva Etapa,
    int CantidadLotesQuePasaron,
    decimal AlimentoTotalKg,
    int MortalidadTotal,
    int? DiasPromedio,
    int? DiasMin,
    int? DiasMax,
    int? DiasEsperadosMinFondepes,
    int? DiasEsperadosMaxFondepes);

/// <summary>Mismo agregado que <see cref="ResumenEtapaGlobal"/> pero por tramo de dieta de Sierra Nevada (ver BandaSierraNevada).</summary>
public sealed record ResumenTramoGlobalSierraNevada(
    string Tramo,
    int CantidadLotesQuePasaron,
    decimal AlimentoTotalKg,
    int MortalidadTotal,
    int? DiasPromedio,
    int? DiasMin,
    int? DiasMax,
    int? DiasEsperados);

/// <summary>
/// Reporte consolidado de TODA la producción activa — no un lote a la vez, sino cómo ha
/// evolucionado la trucha a través de todos los lotes/campañas: cuánto se le dio de comer por
/// etapa, cuánto tardó en crecer, cuántos peces murieron y en qué etapa.
/// </summary>
public sealed record ReporteProduccionGlobalResultado(
    int TotalPecesSembrados,
    int TotalPecesVivos,
    int TotalMortalidad,
    decimal PorcentajeSupervivencia,
    decimal BiomasaTotalActualKg,
    IReadOnlyList<ResumenEtapaGlobal> PorEtapaFondepes,
    IReadOnlyList<ResumenTramoGlobalSierraNevada> PorTramoSierraNevada);
