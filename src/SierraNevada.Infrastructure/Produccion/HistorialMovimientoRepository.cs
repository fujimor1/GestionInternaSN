using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class HistorialMovimientoRepository(IDbConnectionFactory connectionFactory) : IHistorialMovimientoRepository
{
    public async Task<IReadOnlyList<HistorialMovimiento>> ListarPorLoteAsync(int loteId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM historial_movimiento WHERE lote_id = @loteId ORDER BY fecha DESC", connection);
        command.Parameters.AddWithValue("loteId", loteId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<HistorialMovimiento>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<IReadOnlyList<HistorialMovimiento>> ListarEventosEtapaPorLoteAsync(int loteId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            SELECT * FROM historial_movimiento
            WHERE lote_id = @loteId AND etapa_nueva IS NOT NULL
            ORDER BY fecha ASC
            """, connection);
        command.Parameters.AddWithValue("loteId", loteId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<HistorialMovimiento>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(HistorialMovimiento movimiento, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO historial_movimiento
                (lote_id, fecha, tipo_movimiento, descripcion, cantidad_afectada, etapa_anterior, etapa_nueva)
            VALUES
                (@loteId, @fecha, @tipoMovimiento, @descripcion, @cantidadAfectada, @etapaAnterior, @etapaNueva)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("loteId", movimiento.LoteId);
        command.Parameters.AddWithValue("fecha", movimiento.Fecha);
        command.Parameters.AddWithValue("tipoMovimiento", movimiento.TipoMovimiento.ToString());
        command.Parameters.AddWithValue("descripcion", movimiento.Descripcion);
        command.Parameters.AddWithValue("cantidadAfectada", (object?)movimiento.CantidadAfectada ?? DBNull.Value);
        command.Parameters.AddWithValue("etapaAnterior", (object?)movimiento.EtapaAnterior?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("etapaNueva", (object?)movimiento.EtapaNueva?.ToString() ?? DBNull.Value);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static HistorialMovimiento Mapear(NpgsqlDataReader reader) => HistorialMovimiento.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        loteId: reader.GetInt32(reader.GetOrdinal("lote_id")),
        fecha: reader.GetDateTime(reader.GetOrdinal("fecha")),
        tipoMovimiento: Enum.Parse<TipoMovimientoHistorial>(reader.GetString(reader.GetOrdinal("tipo_movimiento"))),
        descripcion: reader.GetString(reader.GetOrdinal("descripcion")),
        cantidadAfectada: reader.GetNullableInt("cantidad_afectada"),
        etapaAnterior: reader.GetNullableEnum<EtapaProductiva>("etapa_anterior"),
        etapaNueva: reader.GetNullableEnum<EtapaProductiva>("etapa_nueva"));
}
