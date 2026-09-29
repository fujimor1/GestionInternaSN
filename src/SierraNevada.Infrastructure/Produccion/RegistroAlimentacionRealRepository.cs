using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class RegistroAlimentacionRealRepository(IDbConnectionFactory connectionFactory) : IRegistroAlimentacionRealRepository
{
    public async Task<IReadOnlyList<RegistroAlimentacionReal>> ListarPorLoteEnRangoAsync(
        int loteId, DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            SELECT * FROM registro_alimentacion_real
            WHERE lote_id = @loteId AND fecha BETWEEN @desde AND @hasta
            ORDER BY fecha ASC
            """, connection);
        command.Parameters.AddWithValue("loteId", loteId);
        command.Parameters.AddWithValue("desde", desde);
        command.Parameters.AddWithValue("hasta", hasta);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<RegistroAlimentacionReal>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<IReadOnlyList<RegistroAlimentacionReal>> ListarPorLoteAsync(int loteId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM registro_alimentacion_real WHERE lote_id = @loteId ORDER BY fecha ASC", connection);
        command.Parameters.AddWithValue("loteId", loteId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<RegistroAlimentacionReal>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(RegistroAlimentacionReal registro, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO registro_alimentacion_real (lote_id, fecha, cantidad_kg_entregada, tipo_alimento)
            VALUES (@loteId, @fecha, @cantidadKgEntregada, @tipoAlimento)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("loteId", registro.LoteId);
        command.Parameters.AddWithValue("fecha", registro.Fecha);
        command.Parameters.AddWithValue("cantidadKgEntregada", registro.CantidadKgEntregada);
        command.Parameters.AddWithValue("tipoAlimento", registro.TipoAlimento.ToString());

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static RegistroAlimentacionReal Mapear(NpgsqlDataReader reader) => RegistroAlimentacionReal.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        loteId: reader.GetInt32(reader.GetOrdinal("lote_id")),
        fecha: reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
        cantidadKgEntregada: reader.GetDecimal(reader.GetOrdinal("cantidad_kg_entregada")),
        tipoAlimento: Enum.Parse<TipoAlimento>(reader.GetString(reader.GetOrdinal("tipo_alimento"))));
}
