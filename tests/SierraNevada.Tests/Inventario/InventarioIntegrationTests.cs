using SierraNevada.Application.Inventario.CasosDeUso;
using SierraNevada.Domain.Inventario;
using SierraNevada.Infrastructure.Inventario;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Tests.Inventario;

public sealed class InventarioIntegrationTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=sierranevada;Username=postgres;Password=1234";

    [Fact]
    public async Task FlujoInventario_IngresoEgresoKardexYReorden_EjecutaCorrectamente()
    {
        // Arrange
        DatabaseMigrator.MigrateDatabase(ConnectionString);
        var connectionFactory = new NpgsqlConnectionFactory(ConnectionString);

        var tipoRepo = new TipoAlimentoRepository(connectionFactory);
        var proveedorRepo = new ProveedorAlimentoRepository(connectionFactory);
        var loteRepo = new LoteAlimentoRepository(connectionFactory);
        var kardexRepo = new KardexAlimentoRepository(connectionFactory);

        var ingresoService = new RegistrarIngresoAlimentoService(tipoRepo, proveedorRepo, loteRepo, kardexRepo);
        var egresoService = new RegistrarEgresoAlimentoService(loteRepo, kardexRepo);
        var reordenService = new CalcularPlanReordenService(tipoRepo, proveedorRepo, kardexRepo);

        var sufijo = Guid.NewGuid().ToString("N")[..6];

        // 1. Crear Tipo de Alimento
        var tipoResult = TipoAlimentoCatalogo.Crear(
            nombre: $"Test Pellet {sufijo}",
            marca: "Nicovita Test",
            calibreMm: 1.5m,
            porcentajeProteina: 46.0m,
            porcentajeGrasa: 14.0m,
            etapaSugerida: "ALEVINAJE",
            costoUnitarioPromedioKg: 7.50m,
            costoAlmacenamientoAnualPorKg: 0.60m);

        Assert.False(tipoResult.IsError);
        int tipoId = await tipoRepo.CrearAsync(tipoResult.Value);

        // 2. Crear Proveedor
        var provResult = ProveedorAlimento.Crear(
            ruc: $"20{Random.Shared.Next(10000000, 99999999)}",
            razonSocial: $"Proveedor Test {sufijo}",
            contactoNombre: "Juan Test",
            telefono: "999888777",
            email: "test@proveedor.com",
            leadTimeDiasPromedio: 4,
            costoOrdenPedido: 60.00m);

        Assert.False(provResult.IsError);
        int provId = await proveedorRepo.CrearAsync(provResult.Value);

        // 3. Registrar Ingreso de Compra (40 sacos de 25kg = 1000 kg a S/ 7.50/kg = S/ 7,500)
        var cmdIngreso = new RegistrarIngresoAlimentoCommand(
            TipoAlimentoId: tipoId,
            ProveedorId: provId,
            CodigoLoteFabrica: $"LOT-TEST-{sufijo}",
            FechaFabricacion: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            FechaVencimiento: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(180)),
            PesoPorSacoKg: 25.0m,
            CantidadSacos: 40,
            PrecioUnitarioKg: 7.50m,
            Observaciones: "Prueba de auditoria automatizada");

        var ingresoResult = await ingresoService.EjecutarAsync(cmdIngreso);
        Assert.False(ingresoResult.IsError);
        Assert.Equal(1000m, ingresoResult.Value.TotalKg);
        Assert.Equal(7500m, ingresoResult.Value.TotalCosto);

        // Verificar saldo en Kardex
        var saldoActual = await kardexRepo.ObtenerSaldoActualPorTipoAlimentoAsync(tipoId);
        Assert.Equal(1000m, saldoActual.TotalKg);
        Assert.Equal(7500m, saldoActual.TotalValorizado);

        // 4. Registrar Egreso por Alimentación (100 kg)
        var cmdEgreso = new RegistrarEgresoAlimentoCommand(
            LoteAlimentoId: ingresoResult.Value.LoteAlimentoId,
            TipoMovimiento: TipoMovimientoKardex.EgresoAlimentacion,
            CantidadKg: 100m,
            Observaciones: "Alimentacion estanque de prueba");

        var egresoResult = await egresoService.EjecutarAsync(cmdEgreso);
        Assert.False(egresoResult.IsError);
        Assert.Equal(900m, egresoResult.Value.StockRestanteKg);

        // Verificar nuevo saldo
        var saldoDespuesEgreso = await kardexRepo.ObtenerSaldoActualPorTipoAlimentoAsync(tipoId);
        Assert.Equal(900m, saldoDespuesEgreso.TotalKg);
        Assert.Equal(6750m, saldoDespuesEgreso.TotalValorizado);

        // 5. Validar que no permita egresar más stock del disponible
        var cmdEgresoExcesivo = new RegistrarEgresoAlimentoCommand(
            LoteAlimentoId: ingresoResult.Value.LoteAlimentoId,
            TipoMovimiento: TipoMovimientoKardex.EgresoAlimentacion,
            CantidadKg: 950m); // Solo quedan 900 kg

        var errorResult = await egresoService.EjecutarAsync(cmdEgresoExcesivo);
        Assert.True(errorResult.IsError);
        Assert.Equal("LoteAlimento.StockInsuficiente", errorResult.FirstError.Code);

        // 6. Verificar cálculo de Reorden (ROP, SS, EOQ)
        var plan = await reordenService.EjecutarAsync(nivelServicioZ: 1.65, diasHistoricoDemanda: 30);
        Assert.NotNull(plan);
        Assert.NotEmpty(plan.Items);

        var itemPlan = plan.Items.FirstOrDefault(i => i.TipoAlimentoId == tipoId);
        Assert.NotNull(itemPlan);
        Assert.Equal(900m, itemPlan.StockActualKg);
        Assert.True(itemPlan.PuntoReordenKg > 0);
        Assert.True(itemPlan.StockSeguridadKg > 0);
        Assert.True(itemPlan.CantidadEconomicaPedidoEoqKg > 0);
    }
}
