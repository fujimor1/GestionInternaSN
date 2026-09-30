namespace SierraNevada.Domain.Produccion.ML;

/// <summary>
/// Motor Adaptativo de Machine Learning basado en Actualización Bayesiana Conjugada
/// (Normal-Normal para variables continuas TGC, K y FCA; Beta-Binomial para tasa de mortalidad).
/// Permite que el sistema aprenda campana tras campaña sin sobreajuste con muestras pequeñas.
/// Documentado en docs/ml-fase3-formulas.md.
/// </summary>
public static class MotorBayesianoAdaptativo
{
    public sealed record EstimacionNormal(double Media, double Varianza, double DesviacionEstandar)
    {
        public double IntervaloInferior95 => Math.Max(0, Media - 1.96 * DesviacionEstandar);
        public double IntervaloSuperior95 => Media + 1.96 * DesviacionEstandar;
    }

    public sealed record EstimacionBeta(double Alfa, double Beta, double TasaEsperada, double Varianza)
    {
        public double IntervaloInferior95 => Math.Max(0, TasaEsperada - 1.96 * Math.Sqrt(Varianza));
        public double IntervaloSuperior95 => Math.Min(1, TasaEsperada + 1.96 * Math.Sqrt(Varianza));
    }

    /// <summary>
    /// Actualización Bayesiana Normal-Normal para parámetros continuos (TGC, FCA, Factor de Condición K).
    /// </summary>
    /// <param name="priorMedia">μ₀: Media inicial (FONDEPES / histórica)</param>
    /// <param name="priorVarianza">σ₀²: Incertidumbre inicial</param>
    /// <param name="muestraMedia">x̄: Promedio observado en los datos recientes</param>
    /// <param name="muestraVarianza">σ²: Varianza del proceso de medición</param>
    /// <param name="nMuestras">n: Cantidad de mediciones u observaciones en la campaña</param>
    public static EstimacionNormal ActualizarNormal(
        double priorMedia,
        double priorVarianza,
        double muestraMedia,
        double muestraVarianza,
        int nMuestras)
    {
        if (nMuestras <= 0 || muestraVarianza <= 0 || priorVarianza <= 0)
        {
            return new EstimacionNormal(priorMedia, priorVarianza, Math.Sqrt(priorVarianza));
        }

        // Fórmula Normal-Normal:
        // μ_post = (σ² * μ₀ + n * σ₀² * x̄) / (σ² + n * σ₀²)
        // σ²_post = (σ² * σ₀²) / (σ² + n * σ₀²)
        double denominador = muestraVarianza + (nMuestras * priorVarianza);
        double mediaPosterior = ((muestraVarianza * priorMedia) + (nMuestras * priorVarianza * muestraMedia)) / denominador;
        double varianzaPosterior = (muestraVarianza * priorVarianza) / denominador;

        return new EstimacionNormal(
            mediaPosterior,
            varianzaPosterior,
            Math.Sqrt(varianzaPosterior)
        );
    }

    /// <summary>
    /// Actualización Bayesiana Beta-Binomial para tasas de mortalidad o supervivencia.
    /// </summary>
    /// <param name="priorAlfa">α₀: Conteo de éxitos a priori</param>
    /// <param name="priorBeta">β₀: Conteo de fracasos a priori</param>
    /// <param name="muertesObservadas">k: Muertes reales observadas en el periodo</param>
    /// <param name="poblacionTotal">n: Peces totales en el periodo</param>
    public static EstimacionBeta ActualizarMortalidadBeta(
        double priorAlfa,
        double priorBeta,
        int muertesObservadas,
        int poblacionTotal)
    {
        if (poblacionTotal <= 0)
        {
            double tasaInicial = priorAlfa / (priorAlfa + priorBeta);
            double varInicial = (priorAlfa * priorBeta) / (Math.Pow(priorAlfa + priorBeta, 2) * (priorAlfa + priorBeta + 1));
            return new EstimacionBeta(priorAlfa, priorBeta, tasaInicial, varInicial);
        }

        double nuevoAlfa = priorAlfa + muertesObservadas;
        double nuevoBeta = priorBeta + (poblacionTotal - muertesObservadas);

        double totalSuma = nuevoAlfa + nuevoBeta;
        double tasaEsperada = nuevoAlfa / totalSuma;
        double varianza = (nuevoAlfa * nuevoBeta) / (Math.Pow(totalSuma, 2) * (totalSuma + 1));

        return new EstimacionBeta(
            nuevoAlfa,
            nuevoBeta,
            tasaEsperada,
            varianza
        );
    }
}
