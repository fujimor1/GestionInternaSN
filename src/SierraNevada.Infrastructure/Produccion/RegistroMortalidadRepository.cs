using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class RegistroMortalidadRepository(IDbConnectionFactory connectionFactory) : IRegistroMortalidadRepository
{
    public async Task<IReadOnlyList<RegistroMortalidad>> ListarPorLoteAsync(int loteId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM registro_mortalidad WHERE lote_id = @loteId ORDER BY fecha ASC", connection);
        command.Parameters.AddWithValue("loteId", loteId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<RegistroMortalidad>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<IReadOnlyList<RegistroMortalidad>> ListarPorLoteEnRangoAsync(
        int loteId, DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            SELECT * FROM registro_mortalidad
            WHERE lote_id = @loteId AND fecha BETWEEN @desde AND @hasta
            ORDER BY fecha ASC
            """, connection);
        command.Parameters.AddWithValue("loteId", loteId);
        command.Parameters.AddWithValue("desde", desde);
        command.Parameters.AddWithValue("hasta", hasta);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<RegistroMortalidad>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(RegistroMortalidad registro, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO registro_mortalidad (lote_id, fecha, cantidad, registrado_por)
            VALUES (@loteId, @fecha, @cantidad, @registradoPor)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("loteId", registro.LoteId);
        command.Parameters.AddWithValue("fecha", registro.Fecha);
        command.Parameters.AddWithValue("cantidad", registro.Cantidad);
        command.Parameters.AddWithValue("registradoPor", registro.RegistradoPor);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static RegistroMortalidad Mapear(NpgsqlDataReader reader) => RegistroMortalidad.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        loteId: reader.GetInt32(reader.GetOrdinal("lote_id")),
        fecha: reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
        cantidad: reader.GetInt32(reader.GetOrdinal("cantidad")),
        registradoPor: reader.GetString(reader.GetOrdinal("registrado_por")));
}
