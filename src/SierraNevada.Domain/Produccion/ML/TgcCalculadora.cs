namespace SierraNevada.Domain.Produccion.ML;

/// <summary>
/// Modelo de Grados-Día (Thermal Growth Coefficient — TGC), estándar internacional
/// en salmonicultura para modelar el crecimiento de trucha arcoíris dependiente de la temperatura del agua.
/// Documentado en docs/ml-fase3-formulas.md.
/// </summary>
public static class TgcCalculadora
{
    /// <summary>
    /// Calcula el TGC observado en una campaña o periodo a partir de los pesos y la suma térmica acumulada.
    /// Fórmula: TGC = ( peso_final^(1/3) - peso_inicial^(1/3) ) / suma_grados_dia * 1000
    /// </summary>
    public static double CalcularTgc(double pesoInicialGramos, double pesoFinalGramos, double sumaGradosDia)
    {
        if (pesoInicialGramos <= 0 || pesoFinalGramos <= 0 || sumaGradosDia <= 0)
            return 0;

        double raizInicial = Math.Cbrt(pesoInicialGramos);
        double raizFinal = Math.Cbrt(pesoFinalGramos);

        return ((raizFinal - raizInicial) / sumaGradosDia) * 1000.0;
    }

    /// <summary>
    /// Proyecta el peso final esperado usando TGC y grados-día acumulados.
    /// Fórmula: W_f^(1/3) = W_i^(1/3) + TGC * suma_grados_dia / 1000
    /// </summary>
    public static double PredecirPesoFinal(double pesoInicialGramos, double tgc, double sumaGradosDia)
    {
        if (pesoInicialGramos <= 0) return 0;
        if (sumaGradosDia <= 0 || tgc <= 0) return pesoInicialGramos;

        double raizInicial = Math.Cbrt(pesoInicialGramos);
        double raizFinal = raizInicial + (tgc * sumaGradosDia / 1000.0);

        if (raizFinal <= 0) return 0;
        return Math.Pow(raizFinal, 3);
    }

    /// <summary>
    /// Calcula la ración total requerida de alimento (kg) para ganar una biomasa dada con un FCA previsto.
    /// Ración (kg) = GananciaBiomasa (kg) * FCA
    /// </summary>
    public static decimal CalcularRacionProyectada(decimal biomasaInicialKg, decimal biomasaFinalKg, decimal fca)
    {
        decimal ganancia = biomasaFinalKg - biomasaInicialKg;
        if (ganancia <= 0 || fca <= 0) return 0;
        return ganancia * fca;
    }
}
