using SierraNevada.Domain.Produccion.ML;

namespace SierraNevada.Tests.MachineLearning;

public sealed class MachineLearningDomainTests
{
    [Fact]
    public void TgcCalculadora_CalculaTgcYPredicePesoCorrectamente()
    {
        // Caso real: trucha de 5g a 20.74g en 30 días a 12°C de temperatura promedio
        double pesoInicial = 5.0;
        double pesoFinal = 20.74;
        double sumaGradosDia = 30.0 * 12.0; // 360 grados-día

        double tgc = TgcCalculadora.CalcularTgc(pesoInicial, pesoFinal, sumaGradosDia);
        Assert.True(tgc > 0.5 && tgc < 4.0, $"TGC calculado ({tgc}) fuera del rango biológico esperado para Oncorhynchus mykiss.");

        // Predicción inversa a partir del TGC
        double pesoPredicho = TgcCalculadora.PredecirPesoFinal(pesoInicial, tgc, sumaGradosDia);
        Assert.Equal(Math.Round(pesoFinal, 1), Math.Round(pesoPredicho, 1));
    }

    [Fact]
    public void MotorBayesianoAdaptativo_NormalNormal_CalibraConConvergenciaCorrecta()
    {
        // Prior: TGC de FONDEPES con media 1.00 y varianza 0.05
        double priorMedia = 1.00;
        double priorVar = 0.05;

        // Muestra observada: media 1.20 con 10 mediciones y varianza de medición 0.02
        double muestraMedia = 1.20;
        double muestraVar = 0.02;
        int n = 10;

        var resultado = MotorBayesianoAdaptativo.ActualizarNormal(priorMedia, priorVar, muestraMedia, muestraVar, n);

        // La media posterior debe estar entre el prior (1.00) y la muestra (1.20), con menor incertidumbre
        Assert.True(resultado.Media > 1.00 && resultado.Media < 1.20);
        Assert.True(resultado.Varianza < priorVar, "La varianza posterior debe ser menor que la del prior al incorporar datos.");
        Assert.True(resultado.IntervaloInferior95 < resultado.Media);
        Assert.True(resultado.IntervaloSuperior95 > resultado.Media);
    }

    [Fact]
    public void MotorBayesianoAdaptativo_BetaBinomial_CalibraTasaMortalidad()
    {
        // Prior: α = 5, β = 95 (tasa esperada del 5%)
        double priorAlfa = 5;
        double priorBeta = 95;

        // Observación: 15 muertes de 500 peces (3% real)
        int muertes = 15;
        int totalPeces = 500;

        var resultado = MotorBayesianoAdaptativo.ActualizarMortalidadBeta(priorAlfa, priorBeta, muertes, totalPeces);

        // La tasa esperada se actualiza correctamente
        Assert.True(resultado.TasaEsperada > 0.025 && resultado.TasaEsperada < 0.045);
        Assert.True(resultado.Varianza < 0.001);
    }
}
