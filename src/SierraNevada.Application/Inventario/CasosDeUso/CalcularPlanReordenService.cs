using SierraNevada.Application.Inventario.Repositories;
using SierraNevada.Domain.Inventario;

namespace SierraNevada.Application.Inventario.CasosDeUso;

public sealed record ParametrosReordenDto(
    int TipoAlimentoId,
    string NombreAlimento,
    string Marca,
    decimal CalibreMm,
    string EtapaSugerida,
    decimal StockActualKg,
    int StockActualSacosAprox,
    decimal StockValorizadoActual,
    decimal DemandaDiariaPromedioKg,
    decimal DesviacionEstandarDemanda,
    decimal DemandaAnualKg,
    int LeadTimeDias,
    decimal NivelServicioZ,
    decimal StockSeguridadKg,
    decimal PuntoReordenKg,
    decimal CantidadEconomicaPedidoEoqKg,
    int CantidadEconomicaPedidoEoqSacos,
    EstadoStockReorden EstadoStock,
    string EstadoStockTexto,
    decimal DiasStockRestante,
    decimal CantidadSugeridaPedirKg,
    int SacosSugeridosPedir,
    string ProveedorRecomendado,
    decimal CostoEstimadoPedido);

public sealed record PlanReordenGlobalResponse(
    IReadOnlyList<ParametrosReordenDto> Items,
    int TotalCriticos,
    int TotalEnReorden,
    int TotalOptimos,
    decimal InversionSugeridaTotal);

public sealed class CalcularPlanReordenService(
    ITipoAlimentoRepository tipoAlimentoRepo,
    IProveedorAlimentoRepository proveedorRepo,
    IKardexAlimentoRepository kardexRepo)
{
    public async Task<PlanReordenGlobalResponse> EjecutarAsync(
        double nivelServicioZ = 1.65, // 95% por defecto
        int diasHistoricoDemanda = 60,
        CancellationToken ct = default)
    {
        var tiposAlimento = await tipoAlimentoRepo.ObtenerTodosAsync(ct);
        var proveedores = await proveedorRepo.ObtenerTodosAsync(ct);
        var items = new List<ParametrosReordenDto>();

        foreach (var tipo in tiposAlimento)
        {
            var saldo = await kardexRepo.ObtenerSaldoActualPorTipoAlimentoAsync(tipo.Id, ct);
            var consumosDiarios = await kardexRepo.ObtenerConsumoDiarioHistoricoAsync(tipo.Id, diasHistoricoDemanda, ct);

            // 1. Demanda diaria promedio (d)
            decimal d = 0;
            decimal sigmaD = 0;

            if (consumosDiarios.Count > 0)
            {
                var valores = consumosDiarios.Select(c => (double)c.TotalKg).ToList();
                double promedio = valores.Average();
                d = (decimal)promedio;

                if (valores.Count > 1)
                {
                    double sumCuadrados = valores.Sum(v => Math.Pow(v - promedio, 2));
                    double varianza = sumCuadrados / (valores.Count - 1);
                    sigmaD = (decimal)Math.Sqrt(varianza);
                }
            }
            else
            {
                // Valor base según calibre/etapa si aún no hay consumo registrado
                d = tipo.CalibreMm switch
                {
                    < 1.0m => 5.0m,
                    < 2.5m => 15.0m,
                    < 3.5m => 35.0m,
                    _ => 60.0m
                };
                sigmaD = d * 0.25m; // 25% de variabilidad supuesta
            }

            if (d <= 0) d = 5.0m;
            if (sigmaD <= 0) sigmaD = d * 0.20m;

            decimal demandaAnualD = d * 365m;

            // 2. Proveedor asociado y parámetros de pedido
            var proveedor = proveedores.FirstOrDefault(p => p.Activo) ?? proveedores.FirstOrDefault();
            int leadTimeL = proveedor?.LeadTimeDiasPromedio ?? 5;
            decimal costoOrdenS = proveedor?.CostoOrdenPedido ?? 50.00m;
            decimal costoAlmacenamientoH = tipo.CostoAlmacenamientoAnualPorKg > 0 ? tipo.CostoAlmacenamientoAnualPorKg : 0.50m;

            // 3. Stock de Seguridad (SS) = Z * sigma_d * sqrt(L)
            decimal ss = (decimal)nivelServicioZ * sigmaD * (decimal)Math.Sqrt(leadTimeL);
            if (ss < 0) ss = 0;

            // 4. Punto de Reorden (ROP) = (d * L) + SS
            decimal rop = (d * leadTimeL) + ss;

            // 5. Cantidad Económica de Pedido (EOQ) = sqrt( (2 * D * S) / H )
            decimal eoq = 0;
            if (costoAlmacenamientoH > 0 && demandaAnualD > 0)
            {
                double eoqCalc = Math.Sqrt((double)(2m * demandaAnualD * costoOrdenS) / (double)costoAlmacenamientoH);
                eoq = (decimal)eoqCalc;
            }
            if (eoq < 50) eoq = 50m; // Mínimo 2 sacos de 25kg

            // 6. Clasificación del estado del stock
            decimal stockActual = saldo.TotalKg;
            EstadoStockReorden estado;
            string estadoTexto;

            if (stockActual <= ss)
            {
                estado = EstadoStockReorden.Critico;
                estadoTexto = "CRÍTICO (Bajo Stock de Seguridad)";
            }
            else if (stockActual <= rop)
            {
                estado = EstadoStockReorden.Reorden;
                estadoTexto = "REORDENAR (Bajo Punto de Reorden)";
            }
            else if (stockActual <= (rop + eoq))
            {
                estado = EstadoStockReorden.Optimo;
                estadoTexto = "ÓPTIMO";
            }
            else
            {
                estado = EstadoStockReorden.Sobrestock;
                estadoTexto = "SOBRESTOCK";
            }

            decimal diasRestantes = d > 0 ? Math.Round(stockActual / d, 1) : 999;

            // 7. Cantidad sugerida a pedir
            decimal cantidadSugeridaKg = 0;
            if (stockActual <= rop)
            {
                cantidadSugeridaKg = Math.Max(eoq, (rop - stockActual) + ss);
                // Redondear a múltiplos de saco de 25 kg
                cantidadSugeridaKg = Math.Ceiling(cantidadSugeridaKg / 25m) * 25m;
            }

            int sacosSugeridos = (int)(cantidadSugeridaKg / 25m);
            decimal costoEstimado = cantidadSugeridaKg * tipo.CostoUnitarioPromedioKg;

            items.Add(new ParametrosReordenDto(
                tipo.Id,
                tipo.Nombre,
                tipo.Marca,
                tipo.CalibreMm,
                tipo.EtapaSugerida,
                Math.Round(stockActual, 2),
                (int)Math.Floor(stockActual / 25m),
                Math.Round(saldo.TotalValorizado, 2),
                Math.Round(d, 2),
                Math.Round(sigmaD, 2),
                Math.Round(demandaAnualD, 2),
                leadTimeL,
                (decimal)nivelServicioZ,
                Math.Round(ss, 2),
                Math.Round(rop, 2),
                Math.Round(eoq, 2),
                (int)Math.Ceiling(eoq / 25m),
                estado,
                estadoTexto,
                diasRestantes,
                cantidadSugeridaKg,
                sacosSugeridos,
                proveedor?.RazonSocial ?? "Proveedor Estándar",
                Math.Round(costoEstimado, 2)
            ));
        }

        int criticos = items.Count(i => i.EstadoStock == EstadoStockReorden.Critico);
        int reorden = items.Count(i => i.EstadoStock == EstadoStockReorden.Reorden);
        int optimos = items.Count(i => i.EstadoStock == EstadoStockReorden.Optimo);
        decimal inversionTotal = items.Sum(i => i.CostoEstimadoPedido);

        return new PlanReordenGlobalResponse(items, criticos, reorden, optimos, inversionTotal);
    }
}
