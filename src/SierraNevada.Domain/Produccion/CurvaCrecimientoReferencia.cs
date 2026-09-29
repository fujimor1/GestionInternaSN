namespace SierraNevada.Domain.Produccion;

/// <summary>Un punto (talla, peso) en un día — sea de una curva esperada o de la serie real.</summary>
public sealed record PuntoCurvaCrecimiento(int Dia, decimal TallaCm, decimal PesoGr);

/// <summary>
/// Cómo debería crecer un pez en talla y peso, en función del TIEMPO — no solo "en qué rango
/// debo estar", sino día a día. Cada marco usa SU PROPIA herramienta, sin forzarlas a coincidir:
/// - FONDEPES: el protocolo no da una fórmula día a día, solo un rango de talla y un rango de
///   días por subetapa (Tabla 5) — se interpola en línea recta dentro de cada tramo, muestreada
///   semanalmente (que es la granularidad real que el protocolo ofrece).
/// - Sierra Nevada: SÍ tiene una fórmula día a día real (columna D-M del Excel: ración, FCA
///   objetivo, ganancia diaria) — se simula literalmente esa fórmula, día por día.
/// Los días son ABSOLUTOS desde una talla de partida teórica (3.5cm) — CalcularCurvaCrecimiento
/// en el motor de calibración los realinea al punto de partida REAL de cada lote.
/// </summary>
public static class CurvaCrecimientoReferencia
{
    private static readonly (decimal TallaDesde, decimal TallaHasta, decimal DiasTramo)[] TramosFondepes =
    [
        (3.5m, 5.0m, 37.5m),   // Alevinaje I: 30-45 días, promedio
        (5.0m, 8.0m, 30m),     // Alevinaje II
        (8.0m, 12.0m, 30m),    // Alevinaje III
        (12.0m, 14.0m, 30m),   // Juveniles I
        (14.0m, 17.0m, 30m),   // Juveniles II
        (17.0m, 20.0m, 60m),   // Engorde I
        (20.0m, 26.0m, 75m),   // Engorde II: 60-90 días, promedio, hasta talla comercial
    ];

    private const int PasoSemanalDias = 7;

    /// <summary>Peso derivado de la talla con K=1.123 fijo — mismo criterio que BANDAS_FONDEPES del frontend.</summary>
    private static decimal PesoDesdeTallaFondepes(decimal tallaCm) => Math.Round(0.01123m * tallaCm * tallaCm * tallaCm, 2);

    public static IReadOnlyList<PuntoCurvaCrecimiento> GenerarFondepesSemanal()
    {
        var resultado = new List<PuntoCurvaCrecimiento> { new(0, TramosFondepes[0].TallaDesde, PesoDesdeTallaFondepes(TramosFondepes[0].TallaDesde)) };
        var diaAcumulado = 0m;

        foreach (var tramo in TramosFondepes)
        {
            var pasos = (int)Math.Ceiling(tramo.DiasTramo / PasoSemanalDias);
            for (var i = 1; i <= pasos; i++)
            {
                var diaEnTramo = Math.Min(i * PasoSemanalDias, tramo.DiasTramo);
                var fraccion = tramo.DiasTramo > 0 ? diaEnTramo / tramo.DiasTramo : 1m;
                var talla = tramo.TallaDesde + (tramo.TallaHasta - tramo.TallaDesde) * fraccion;
                var dia = (int)Math.Round(diaAcumulado + diaEnTramo);

                resultado.Add(new PuntoCurvaCrecimiento(dia, talla, PesoDesdeTallaFondepes(talla)));
            }

            diaAcumulado += tramo.DiasTramo;
        }

        return resultado;
    }

    /// <summary>Réplica exacta de las fórmulas de columna G/I del Excel real (ver docs sección 4.1/4.2).</summary>
    private static decimal RacionGInterpolada(decimal talla) => talla switch
    {
        < 4m => 6.0m,
        <= 5.01m => 5.0m,
        <= 5.61m => 4.5m, // interpolado — hueco real de la fórmula del Excel
        <= 7.07m => 4.0m,
        <= 9.45m => 3.5m,
        <= 11.82m => 2.8m,
        <= 14.18m => 2.5m,
        <= 16.54m => 2.35m, // interpolado — hueco real de la fórmula del Excel
        <= 18.9m => 2.2m,
        <= 21.26m => 1.9m,
        <= 23.63m => 1.5m,
        <= 25.97m => 1.4m,
        <= 30m => 1.2m,
        _ => 1.0m,
    };

    private static decimal FcaObjetivoI(decimal talla) => talla switch
    {
        <= 7.07m => 1.0m,
        <= 11.82m => 1.1m,
        <= 18.9m => 1.13m,
        _ => 1.14m,
    };

    private static decimal TallaDesdePeso(decimal pesoG) => (decimal)Math.Pow((double)(pesoG / 0.01123m), 1.0 / 3.0);

    public static IReadOnlyList<PuntoCurvaCrecimiento> GenerarSierraNevadaDiaria()
    {
        var pesoG = 0.01123m * 3.5m * 3.5m * 3.5m; // talla 3.5cm, K=1.123 — misma arrancada que asume el Excel
        var resultado = new List<PuntoCurvaCrecimiento> { new(0, 3.5m, Math.Round(pesoG, 2)) };

        for (var dia = 1; dia <= 400; dia++)
        {
            var talla = TallaDesdePeso(pesoG);
            if (talla >= 30m)
                break;

            var pct = RacionGInterpolada(talla);
            var fca = FcaObjetivoI(talla);
            var ganancia = (pesoG * pct / 100m) / fca;
            pesoG += ganancia;

            resultado.Add(new PuntoCurvaCrecimiento(dia, Math.Round(TallaDesdePeso(pesoG), 2), Math.Round(pesoG, 2)));
        }

        return resultado;
    }
}
