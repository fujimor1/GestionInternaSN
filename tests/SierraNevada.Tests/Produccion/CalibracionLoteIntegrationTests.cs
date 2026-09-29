using SierraNevada.Application.Produccion.Calibracion;
using SierraNevada.Application.Produccion.CasosDeUso;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;
using SierraNevada.Infrastructure.Produccion;

namespace SierraNevada.Tests.Produccion;

/// <summary>
/// Test de integración contra PostgreSQL real (no mocks) — verifica que el motor de
/// calibración (docs/investigacion-parametros-produccion.md sección 8.2, fase 2) calcule K,
/// FCA, mortalidad y duración por etapa correctamente a partir de datos capturados reales,
/// ejercitando toda la cadena: casos de uso → repositorios ADO.NET → PostgreSQL → de vuelta.
///
/// Requiere una base local `sierranevada` corriendo (misma que appsettings.Development.json
/// de la API) — ver docs/arquitectura-tecnica.md sección 3.
/// </summary>
public sealed class CalibracionLoteIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=sierranevada;Username=postgres;Password=1234";

    [Fact]
    public async Task CalcularAsync_ConDatosReales_ProduceMetricasCorrectas()
    {
        // Arrange — misma base que usa la API, migrada si hiciera falta (idempotente).
        DatabaseMigrator.MigrateDatabase(ConnectionString);
        var connectionFactory = new NpgsqlConnectionFactory(ConnectionString);

        var unidadRepo = new UnidadProduccionRepository(connectionFactory);
        var bastidorRepo = new BastidorRepository(connectionFactory);
        var campañaRepo = new CampañaRepository(connectionFactory);
        var loteRepo = new LoteRepository(connectionFactory);
        var muestreoRepo = new MuestreoRepository(connectionFactory);
        var alimentacionRepo = new RegistroAlimentacionRealRepository(connectionFactory);
        var mortalidadRepo = new RegistroMortalidadRepository(connectionFactory);
        var historialRepo = new HistorialMovimientoRepository(connectionFactory);
        var tablaReferenciaRepo = new TablaReferenciaVersionRepository(connectionFactory);

        var crearCampañaService = new CrearCampañaConLotesService(campañaRepo, loteRepo, unidadRepo, bastidorRepo, historialRepo);
        var registrarMuestreoService = new RegistrarMuestreoService(loteRepo, muestreoRepo, historialRepo);
        var registrarAlimentacionService = new RegistrarAlimentacionRealService(loteRepo, alimentacionRepo);
        var registrarMortalidadService = new RegistrarMortalidadService(loteRepo, mortalidadRepo, historialRepo);
        var cambiarEtapaService = new CambiarEtapaService(loteRepo, historialRepo);
        var calcularCalibracionService = new CalcularCalibracionLoteService(
            loteRepo, muestreoRepo, alimentacionRepo, mortalidadRepo, historialRepo, unidadRepo, tablaReferenciaRepo);

        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // Unidad de producción: 5m x 3m x 2m = 30 m³ (volumen usado para densidad real).
        var unidadResult = UnidadProduccion.Crear(
            codigo: $"TEST-JAULA-{sufijo}", TipoUnidadProduccion.Jaula, TipoJaula.Juvenil, FormaUnidad.Rectangular,
            largoM: 5, anchoM: 3, diametroM: null, altoM: 2, densidadSiembraKgM3: 15);
        Assert.False(unidadResult.IsError);
        var unidadId = await unidadRepo.CrearAsync(unidadResult.Value);

        // Campaña + Lote: 1000 alevines sembrados el 2026-01-01, peso/talla inicial 5g/4cm.
        var campañaResult = await crearCampañaService.EjecutarAsync(
            codigoCampaña: $"TEST-CAMP-{sufijo}",
            fechaSiembra: new DateOnly(2026, 1, 1),
            pesoPromedioInicialGr: 5,
            tallaPromedioInicialCm: 4,
            proveedor: "Proveedor de prueba",
            observaciones: null,
            distribuciones: [new DistribucionInicial(unidadId, null, 1000)]);

        Assert.False(campañaResult.IsError);
        var loteId = campañaResult.Value.LoteIds[0];

        // Muestreo 1 (14 días después, sin bajas registradas todavía → 1000 vivos): 8cm, 5.75g
        // → K = 100*5.75/8³ = 1.123 (coincide con el K del Excel de la empresa). La cantidad de
        // peces vivos ya NO la reporta el operario — el servicio la toma del lote (ver Lote.ActualizarConMuestreo).
        var muestreo1Result = await registrarMuestreoService.EjecutarAsync(
            loteId, new DateOnly(2026, 1, 15), pesoPromedioMuestreadoGr: 5.75m, tallaPromedioMuestreadaCm: 8,
            numeroPecesMuestreados: 30, registradoPor: "test");
        Assert.False(muestreo1Result.IsError);

        // 50 bajas registradas entre los dos muestreos → el lote queda en 950 vivos.
        var mortalidadResult = await registrarMortalidadService.EjecutarAsync(
            loteId, new DateOnly(2026, 1, 20), cantidad: 50, registradoPor: "test");
        Assert.False(mortalidadResult.IsError);

        // 3 entregas de alimento real entre los dos muestreos: 2kg + 2kg + 2kg = 6kg.
        foreach (var fecha in new[] { new DateOnly(2026, 1, 20), new DateOnly(2026, 1, 25), new DateOnly(2026, 1, 30) })
        {
            var alimentacionResult = await registrarAlimentacionService.EjecutarAsync(loteId, fecha, 2m, TipoAlimento.Inicio);
            Assert.False(alimentacionResult.IsError);
        }

        // Muestreo 2 (17 días después, con las 50 bajas ya registradas → 950 vivos): 12cm, 20.74g
        // (redondeado a 2 decimales, igual que la columna NUMERIC(10,2) de la BD) → K = 100*20.74/12³ ≈ 1.2.
        var muestreo2Result = await registrarMuestreoService.EjecutarAsync(
            loteId, new DateOnly(2026, 2, 1), pesoPromedioMuestreadoGr: 20.74m, tallaPromedioMuestreadaCm: 12,
            numeroPecesMuestreados: 30, registradoPor: "test");
        Assert.False(muestreo2Result.IsError);

        // Cambio de etapa 4 días después: AlevinajeI (35 días desde la siembra) → AlevinajeII (en curso).
        var cambioEtapaResult = await cambiarEtapaService.EjecutarAsync(loteId, EtapaProductiva.AlevinajeII, new DateOnly(2026, 2, 5));
        Assert.False(cambioEtapaResult.IsError);

        // Act
        var calibracionResult = await calcularCalibracionService.CalcularAsync(loteId);

        // Assert
        Assert.False(calibracionResult.IsError);
        var calibracion = calibracionResult.Value;

        // --- Factor de Condición (K) real por muestreo ---
        Assert.Equal(2, calibracion.SerieFactorCondicion.Count);
        Assert.Equal(1.123m, Math.Round(calibracion.SerieFactorCondicion[0].FactorK, 3));
        Assert.Equal(1.200m, Math.Round(calibracion.SerieFactorCondicion[1].FactorK, 3));

        // --- Duración real por etapa ---
        Assert.Equal(2, calibracion.DuracionesPorEtapa.Count);
        var alevinajeI = calibracion.DuracionesPorEtapa[0];
        Assert.Equal(EtapaProductiva.AlevinajeI, alevinajeI.Etapa);
        Assert.Equal(new DateOnly(2026, 1, 1), alevinajeI.FechaInicio);
        Assert.Equal(new DateOnly(2026, 2, 5), alevinajeI.FechaFin);
        Assert.Equal(35, alevinajeI.DiasReales);

        var alevinajeII = calibracion.DuracionesPorEtapa[1];
        Assert.Equal(EtapaProductiva.AlevinajeII, alevinajeII.Etapa);
        Assert.Equal(new DateOnly(2026, 2, 5), alevinajeII.FechaInicio);
        Assert.Null(alevinajeII.FechaFin);
        Assert.Null(alevinajeII.DiasReales);

        // --- FCA real entre los dos muestreos ---
        Assert.Single(calibracion.FcaPorPeriodo);
        var fcaPeriodo = calibracion.FcaPorPeriodo[0];
        Assert.Equal(6m, fcaPeriodo.AlimentoAcumuladoKg);

        var biomasaMuestreo1 = 1000 * 5.75m / 1000m; // 5.75 kg — todavía sin bajas registradas
        var biomasaMuestreo2 = 950 * 20.74m / 1000m; // 19.703 kg — con las 50 bajas ya descontadas
        var gananciaEsperada = biomasaMuestreo2 - biomasaMuestreo1;
        Assert.Equal(gananciaEsperada, fcaPeriodo.GananciaBiomasaKg);
        Assert.Equal(Math.Round(6m / gananciaEsperada, 4), Math.Round(fcaPeriodo.FcaReal!.Value, 4));

        // --- Mortalidad real ---
        Assert.Equal(50, calibracion.Mortalidad.TotalBajas);
        Assert.Equal(1000, calibracion.Mortalidad.CantidadInicial);
        Assert.Equal(5.0m, calibracion.Mortalidad.PorcentajeAcumulado);

        // --- Densidad real (biomasa del último muestreo / volumen de la unidad) ---
        Assert.Equal(biomasaMuestreo2, calibracion.Densidad.BiomasaActualKg);
        Assert.Equal(30m, calibracion.Densidad.VolumenM3);
        Assert.Equal(Math.Round(biomasaMuestreo2 / 30m, 5), Math.Round(calibracion.Densidad.DensidadKgM3!.Value, 5));
    }
}
