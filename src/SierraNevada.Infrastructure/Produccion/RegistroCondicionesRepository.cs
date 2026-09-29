using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class RegistroCondicionesRepository(IDbConnectionFactory connectionFactory) : IRegistroCondicionesRepository
{
    public async Task<IReadOnlyList<RegistroCondiciones>> ListarPorLoteAsync(int loteId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM registro_condiciones WHERE lote_id = @loteId ORDER BY fecha ASC", connection);
        command.Parameters.AddWithValue("loteId", loteId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<RegistroCondiciones>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(RegistroCondiciones registro, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO registro_condiciones (lote_id, fecha, temp_agua_c, ph, oxigeno_mg_l, amoniaco_mg_l)
            VALUES (@loteId, @fecha, @tempAguaC, @ph, @oxigenoMgL, @amoniacoMgL)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("loteId", registro.LoteId);
        command.Parameters.AddWithValue("fecha", registro.Fecha);
        command.Parameters.AddWithValue("tempAguaC", (object?)registro.TempAguaC ?? DBNull.Value);
        command.Parameters.AddWithValue("ph", (object?)registro.Ph ?? DBNull.Value);
        command.Parameters.AddWithValue("oxigenoMgL", (object?)registro.OxigenoMgL ?? DBNull.Value);
        command.Parameters.AddWithValue("amoniacoMgL", (object?)registro.AmoniacoMgL ?? DBNull.Value);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static RegistroCondiciones Mapear(NpgsqlDataReader reader) => RegistroCondiciones.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        loteId: reader.GetInt32(reader.GetOrdinal("lote_id")),
        fecha: reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
        tempAguaC: reader.GetNullableDecimal("temp_agua_c"),
        ph: reader.GetNullableDecimal("ph"),
        oxigenoMgL: reader.GetNullableDecimal("oxigeno_mg_l"),
        amoniacoMgL: reader.GetNullableDecimal("amoniaco_mg_l"));
}
