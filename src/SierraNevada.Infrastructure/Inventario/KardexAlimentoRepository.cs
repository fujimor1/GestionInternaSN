using Npgsql;
using SierraNevada.Application.Inventario.Repositories;
using SierraNevada.Domain.Inventario;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Inventario;

public sealed class KardexAlimentoRepository(IDbConnectionFactory connectionFactory) : IKardexAlimentoRepository
{
    public async Task<IReadOnlyList<KardexAlimentoMovimiento>> ObtenerMovimientosAsync(
        int? tipoAlimentoId = null,
        int? loteAlimentoId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        var sql = "SELECT * FROM kardex_alimento_movimiento WHERE 1=1";
        if (tipoAlimentoId.HasValue) sql += " AND tipo_alimento_id = @tipoAlimentoId";
        if (loteAlimentoId.HasValue) sql += " AND lote_alimento_id = @loteAlimentoId";
        if (desde.HasValue) sql += " AND fecha_movimiento >= @desde";
        if (hasta.HasValue) sql += " AND fecha_movimiento <= @hasta";
        sql += " ORDER BY fecha_movimiento DESC, id DESC";

        await using var command = new NpgsqlCommand(sql, connection);
        if (tipoAlimentoId.HasValue) command.Parameters.AddWithValue("tipoAlimentoId", tipoAlimentoId.Value);
        if (loteAlimentoId.HasValue) command.Parameters.AddWithValue("loteAlimentoId", loteAlimentoId.Value);
        if (desde.HasValue) command.Parameters.AddWithValue("desde", desde.Value);
        if (hasta.HasValue) command.Parameters.AddWithValue("hasta", hasta.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<KardexAlimentoMovimiento>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<KardexResumenSaldo> ObtenerSaldoActualPorTipoAlimentoAsync(int tipoAlimentoId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        // El saldo actual por tipo de alimento es la suma de los lotes de alimento activos con stock
        await using var command = new NpgsqlCommand("""
            SELECT 
                COALESCE(SUM(stock_kg_actual), 0) AS total_kg,
                COALESCE(SUM(stock_kg_actual * precio_unitario_kg), 0) AS total_valorizado
            FROM lote_alimento
            WHERE tipo_alimento_id = @tipoAlimentoId AND activo = TRUE
            """, connection);

        command.Parameters.AddWithValue("tipoAlimentoId", tipoAlimentoId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            decimal totalKg = reader.GetDecimal(reader.GetOrdinal("total_kg"));
            decimal totalValorizado = reader.GetDecimal(reader.GetOrdinal("total_valorizado"));
            return new KardexResumenSaldo(totalKg, totalValorizado);
        }

        return new KardexResumenSaldo(0, 0);
    }

    public async Task<IReadOnlyList<ConsumoHistoricoDiario>> ObtenerConsumoDiarioHistoricoAsync(
        int? tipoAlimentoId = null,
        int diasAtras = 90,
        CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        var sql = """
            SELECT 
                CAST(fecha_movimiento AS DATE) AS fecha,
                ABS(SUM(cantidad_kg)) AS total_kg
            FROM kardex_alimento_movimiento
            WHERE tipo_movimiento IN ('EGRESO_ALIMENTACION', 'EgresoAlimentacion')
              AND fecha_movimiento >= NOW() - INTERVAL '1 day' * @diasAtras
            """;

        if (tipoAlimentoId.HasValue)
            sql += " AND tipo_alimento_id = @tipoAlimentoId";

        sql += " GROUP BY CAST(fecha_movimiento AS DATE) ORDER BY fecha ASC";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("diasAtras", diasAtras);
        if (tipoAlimentoId.HasValue)
            command.Parameters.AddWithValue("tipoAlimentoId", tipoAlimentoId.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<ConsumoHistoricoDiario>();
        while (await reader.ReadAsync(ct))
        {
            var fecha = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha"));
            var totalKg = reader.GetDecimal(reader.GetOrdinal("total_kg"));
            resultado.Add(new ConsumoHistoricoDiario(fecha, totalKg));
        }

        return resultado;
    }

    public async Task<int> RegistrarMovimientoAsync(KardexAlimentoMovimiento movimiento, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO kardex_alimento_movimiento (
                tipo_alimento_id, lote_alimento_id, fecha_movimiento, tipo_movimiento,
                cantidad_kg, costo_unitario_kg, costo_total, saldo_stock_kg, saldo_valorizado,
                lote_produccion_id, observaciones, usuario_id
            )
            VALUES (
                @tipoAlimentoId, @loteAlimentoId, @fechaMovimiento, @tipoMovimiento,
                @cantidadKg, @costoUnitarioKg, @costoTotal, @saldoStockKg, @saldoValorizado,
                @loteProduccionId, @observaciones, @usuarioId
            )
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("tipoAlimentoId", movimiento.TipoAlimentoId);
        command.Parameters.AddWithValue("loteAlimentoId", movimiento.LoteAlimentoId);
        command.Parameters.AddWithValue("fechaMovimiento", movimiento.FechaMovimiento);
        command.Parameters.AddWithValue("tipoMovimiento", movimiento.TipoMovimiento.ToString());
        command.Parameters.AddWithValue("cantidadKg", movimiento.CantidadKg);
        command.Parameters.AddWithValue("costoUnitarioKg", movimiento.CostoUnitarioKg);
        command.Parameters.AddWithValue("costoTotal", movimiento.CostoTotal);
        command.Parameters.AddWithValue("saldoStockKg", movimiento.SaldoStockKg);
        command.Parameters.AddWithValue("saldoValorizado", movimiento.SaldoValorizado);
        command.Parameters.AddWithValue("loteProduccionId", (object?)movimiento.LoteProduccionId ?? DBNull.Value);
        command.Parameters.AddWithValue("observaciones", (object?)movimiento.Observaciones ?? DBNull.Value);
        command.Parameters.AddWithValue("usuarioId", (object?)movimiento.UsuarioId ?? DBNull.Value);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static KardexAlimentoMovimiento Mapear(NpgsqlDataReader reader) => KardexAlimentoMovimiento.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        tipoAlimentoId: reader.GetInt32(reader.GetOrdinal("tipo_alimento_id")),
        loteAlimentoId: reader.GetInt32(reader.GetOrdinal("lote_alimento_id")),
        fechaMovimiento: reader.GetDateTime(reader.GetOrdinal("fecha_movimiento")),
        tipoMovimiento: Enum.Parse<TipoMovimientoKardex>(reader.GetString(reader.GetOrdinal("tipo_movimiento"))),
        cantidadKg: reader.GetDecimal(reader.GetOrdinal("cantidad_kg")),
        costoUnitarioKg: reader.GetDecimal(reader.GetOrdinal("costo_unitario_kg")),
        costoTotal: reader.GetDecimal(reader.GetOrdinal("costo_total")),
        saldoStockKg: reader.GetDecimal(reader.GetOrdinal("saldo_stock_kg")),
        saldoValorizado: reader.GetDecimal(reader.GetOrdinal("saldo_valorizado")),
        loteProduccionId: reader.GetNullableInt("lote_produccion_id"),
        observaciones: reader.GetNullableString("observaciones"),
        usuarioId: reader.GetNullableInt("usuario_id"));
}
