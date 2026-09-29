using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.Reportes;

/// <summary>
/// Recorre TODOS los lotes activos y cruza cada registro de alimentación/mortalidad contra el
/// historial de cambios de etapa de su propio lote — ninguna de las dos tablas guarda la etapa
/// en el momento del registro, así que se infiere por fecha (mismo principio que el motor de
/// calibración por lote, pero sumado sobre toda la producción en vez de un lote a la vez).
/// </summary>
public sealed class CalcularReporteProduccionGlobalService(
    ILoteRepository loteRepository,
    IMuestreoRepository muestreoRepository,
    IRegistroAlimentacionRealRepository alimentacionRepository,
    IRegistroMortalidadRepository mortalidadRepository,
    IHistorialMovimientoRepository historialRepository)
{
    private sealed record Intervalo<TEtapa>(TEtapa Etapa, DateOnly Inicio, DateOnly? Fin);

    private sealed class Acumulador
    {
        public int CantidadLotesQuePasaron;
        public decimal AlimentoTotalKg;
        public int MortalidadTotal;
        public readonly List<int> DiasTerminados = [];
    }

    public async Task<ReporteProduccionGlobalResultado> CalcularAsync(CancellationToken ct = default)
    {
        var lotes = await loteRepository.ListarActivosAsync(ct);

        var totalSembrados = lotes.Where(l => l.LotePadreId is null).Sum(l => l.CantidadInicial);
        var totalVivos = lotes.Sum(l => l.CantidadTotalPeces);
        var biomasaTotal = lotes.Sum(l => l.BiomasaActualKg);

        var acumEtapa = Enum.GetValues<EtapaProductiva>().ToDictionary(e => e, _ => new Acumulador());
        var acumTramo = new Dictionary<string, Acumulador>();
        var totalMortalidad = 0;

        foreach (var lote in lotes)
        {
            var eventos = await historialRepository.ListarEventosEtapaPorLoteAsync(lote.Id, ct);
            var alimentacion = await alimentacionRepository.ListarPorLoteAsync(lote.Id, ct);
            var mortalidad = await mortalidadRepository.ListarPorLoteAsync(lote.Id, ct);
            var muestreos = await muestreoRepository.ListarPorLoteAsync(lote.Id, ct);

            totalMortalidad += mortalidad.Sum(m => m.Cantidad);

            var intervalosEtapa = ConstruirIntervalosEtapa(eventos);
            AcumularIntervalos(intervalosEtapa, alimentacion, mortalidad, acumEtapa);

            var intervalosTramo = ConstruirIntervalosTramo(muestreos);
            foreach (var intervalo in intervalosTramo)
                acumTramo.TryAdd(intervalo.Etapa, new Acumulador());
            AcumularIntervalos(intervalosTramo, alimentacion, mortalidad, acumTramo);
        }

        var porEtapa = acumEtapa
            .Where(kv => kv.Value.CantidadLotesQuePasaron > 0)
            .OrderBy(kv => kv.Key)
            .Select(kv =>
            {
                var esperado = EtapaProductivaCalculadora.DuracionEsperadaFondepes(kv.Key);
                var dias = kv.Value.DiasTerminados;
                return new ResumenEtapaGlobal(
                    kv.Key, kv.Value.CantidadLotesQuePasaron, kv.Value.AlimentoTotalKg, kv.Value.MortalidadTotal,
                    dias.Count > 0 ? (int)Math.Round(dias.Average()) : null,
                    dias.Count > 0 ? dias.Min() : null,
                    dias.Count > 0 ? dias.Max() : null,
                    esperado?.MinDias, esperado?.MaxDias);
            })
            .ToList();

        var porTramo = acumTramo
            .Select(kv =>
            {
                var dias = kv.Value.DiasTerminados;
                return new ResumenTramoGlobalSierraNevada(
                    kv.Key, kv.Value.CantidadLotesQuePasaron, kv.Value.AlimentoTotalKg, kv.Value.MortalidadTotal,
                    dias.Count > 0 ? (int)Math.Round(dias.Average()) : null,
                    dias.Count > 0 ? dias.Min() : null,
                    dias.Count > 0 ? dias.Max() : null,
                    BandaSierraNevada.DiasEsperados.GetValueOrDefault(kv.Key));
            })
            .OrderBy(r => BandaSierraNevada.DiasEsperados.Keys.ToList().IndexOf(r.Tramo))
            .ToList();

        var supervivencia = totalSembrados > 0 ? 1m - (decimal)totalMortalidad / totalSembrados : 1m;

        return new ReporteProduccionGlobalResultado(
            totalSembrados, totalVivos, totalMortalidad, supervivencia, biomasaTotal, porEtapa, porTramo);
    }

    private static List<Intervalo<EtapaProductiva>> ConstruirIntervalosEtapa(IReadOnlyList<HistorialMovimiento> eventos)
    {
        var resultado = new List<Intervalo<EtapaProductiva>>();
        for (var i = 0; i < eventos.Count; i++)
        {
            var inicio = DateOnly.FromDateTime(eventos[i].Fecha);
            DateOnly? fin = i + 1 < eventos.Count ? DateOnly.FromDateTime(eventos[i + 1].Fecha) : null;
            resultado.Add(new Intervalo<EtapaProductiva>(eventos[i].EtapaNueva!.Value, inicio, fin));
        }

        return resultado;
    }

    private static List<Intervalo<string>> ConstruirIntervalosTramo(IReadOnlyList<Muestreo> muestreos)
    {
        var resultado = new List<Intervalo<string>>();
        if (muestreos.Count == 0)
            return resultado;

        string? tramoActual = null;
        var inicioTramo = default(DateOnly);

        foreach (var m in muestreos)
        {
            var tramo = BandaSierraNevada.Para(m.TallaPromedioMuestreadaCm);
            if (tramo != tramoActual)
            {
                if (tramoActual is not null)
                    resultado.Add(new Intervalo<string>(tramoActual, inicioTramo, m.Fecha));

                tramoActual = tramo;
                inicioTramo = m.Fecha;
            }
        }

        resultado.Add(new Intervalo<string>(tramoActual!, inicioTramo, null));
        return resultado;
    }

    private static void AcumularIntervalos<TEtapa>(
        List<Intervalo<TEtapa>> intervalos,
        IReadOnlyList<RegistroAlimentacionReal> alimentacion,
        IReadOnlyList<RegistroMortalidad> mortalidad,
        Dictionary<TEtapa, Acumulador> acumuladores)
        where TEtapa : notnull
    {
        foreach (var intervalo in intervalos)
        {
            var acum = acumuladores[intervalo.Etapa];
            acum.CantidadLotesQuePasaron++;

            if (intervalo.Fin is not null)
                acum.DiasTerminados.Add(intervalo.Fin.Value.DayNumber - intervalo.Inicio.DayNumber);

            acum.AlimentoTotalKg += alimentacion
                .Where(a => DentroDelIntervalo(a.Fecha, intervalo.Inicio, intervalo.Fin))
                .Sum(a => a.CantidadKgEntregada);

            acum.MortalidadTotal += mortalidad
                .Where(m => DentroDelIntervalo(m.Fecha, intervalo.Inicio, intervalo.Fin))
                .Sum(m => m.Cantidad);
        }
    }

    private static bool DentroDelIntervalo(DateOnly fecha, DateOnly inicio, DateOnly? fin)
        => fecha >= inicio && (fin is null || fecha < fin.Value);
}
