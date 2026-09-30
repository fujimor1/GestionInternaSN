using Npgsql;
using SierraNevada.Application.Inventario.Repositories;
using SierraNevada.Domain.Inventario;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Inventario;

public sealed class LoteAlimentoRepository(IDbConnectionFactory connectionFactory) : ILoteAlimentoRepository
{
    public async Task<IReadOnlyList<LoteAlimento>> ObtenerTodosAsync(bool soloConStock = false, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        string sql = soloConStock
            ? "SELECT * FROM lote_alimento WHERE activo = TRUE AND stock_kg_actual > 0 ORDER BY fecha_vencimiento ASC"
            : "SELECT * FROM lote_alimento WHERE activo = TRUE ORDER BY fecha_recepcion DESC";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<LoteAlimento>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<IReadOnlyList<LoteAlimento>> ObtenerPorTipoAlimentoAsync(int tipoAlimentoId, bool soloConStock = true, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        string sql = soloConStock
            ? "SELECT * FROM lote_alimento WHERE tipo_alimento_id = @tipoAlimentoId AND activo = TRUE AND stock_kg_actual > 0 ORDER BY fecha_vencimiento ASC"
            : "SELECT * FROM lote_alimento WHERE tipo_alimento_id = @tipoAlimentoId AND activo = TRUE ORDER BY fecha_recepcion DESC";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tipoAlimentoId", tipoAlimentoId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<LoteAlimento>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<LoteAlimento?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM lote_alimento WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<int> CrearAsync(LoteAlimento lote, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO lote_alimento (
                tipo_alimento_id, proveedor_id, codigo_lote_fabrica, fecha_fabricacion,
                fecha_vencimiento, peso_por_saco_kg, cantidad_sacos_ingresados, cantidad_sacos_actuales,
                stock_kg_actual, precio_unitario_kg, fecha_recepcion, activo
            )
            VALUES (
                @tipoAlimentoId, @proveedorId, @codigoLoteFabrica, @fechaFabricacion,
                @fechaVencimiento, @pesoPorSacoKg, @cantidadSacosIngresados, @cantidadSacosActuales,
                @stockKgActual, @precioUnitarioKg, @fechaRecepcion, @activo
            )
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("tipoAlimentoId", lote.TipoAlimentoId);
        command.Parameters.AddWithValue("proveedorId", lote.ProveedorId);
        command.Parameters.AddWithValue("codigoLoteFabrica", lote.CodigoLoteFabrica);
        command.Parameters.AddWithValue("fechaFabricacion", (object?)lote.FechaFabricacion ?? DBNull.Value);
        command.Parameters.AddWithValue("fechaVencimiento", lote.FechaVencimiento);
        command.Parameters.AddWithValue("pesoPorSacoKg", lote.PesoPorSacoKg);
        command.Parameters.AddWithValue("cantidadSacosIngresados", lote.CantidadSacosIngresados);
        command.Parameters.AddWithValue("cantidadSacosActuales", lote.CantidadSacosActuales);
        command.Parameters.AddWithValue("stockKgActual", lote.StockKgActual);
        command.Parameters.AddWithValue("precioUnitarioKg", lote.PrecioUnitarioKg);
        command.Parameters.AddWithValue("fechaRecepcion", lote.FechaRecepcion);
        command.Parameters.AddWithValue("activo", lote.Activo);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task ActualizarStockAsync(LoteAlimento lote, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            UPDATE lote_alimento SET
                cantidad_sacos_actuales = @cantidadSacosActuales,
                stock_kg_actual = @stockKgActual,
                activo = @activo
            WHERE id = @id
            """, connection);

        command.Parameters.AddWithValue("id", lote.Id);
        command.Parameters.AddWithValue("cantidadSacosActuales", lote.CantidadSacosActuales);
        command.Parameters.AddWithValue("stockKgActual", lote.StockKgActual);
        command.Parameters.AddWithValue("activo", lote.Activo);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static LoteAlimento Mapear(NpgsqlDataReader reader) => LoteAlimento.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        tipoAlimentoId: reader.GetInt32(reader.GetOrdinal("tipo_alimento_id")),
        proveedorId: reader.GetInt32(reader.GetOrdinal("proveedor_id")),
        codigoLoteFabrica: reader.GetString(reader.GetOrdinal("codigo_lote_fabrica")),
        fechaFabricacion: reader.GetNullableDateOnly("fecha_fabricacion"),
        fechaVencimiento: reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_vencimiento")),
        pesoPorSacoKg: reader.GetDecimal(reader.GetOrdinal("peso_por_saco_kg")),
        cantidadSacosIngresados: reader.GetInt32(reader.GetOrdinal("cantidad_sacos_ingresados")),
        cantidadSacosActuales: reader.GetInt32(reader.GetOrdinal("cantidad_sacos_actuales")),
        stockKgActual: reader.GetDecimal(reader.GetOrdinal("stock_kg_actual")),
        precioUnitarioKg: reader.GetDecimal(reader.GetOrdinal("precio_unitario_kg")),
        fechaRecepcion: reader.GetDateTime(reader.GetOrdinal("fecha_recepcion")),
        activo: reader.GetBoolean(reader.GetOrdinal("activo")));
}
