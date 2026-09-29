using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class CampañaRepository(IDbConnectionFactory connectionFactory) : ICampañaRepository
{
    public async Task<Campaña?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM campania WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<Campaña?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM campania WHERE codigo = @codigo", connection);
        command.Parameters.AddWithValue("codigo", codigo);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<IReadOnlyList<Campaña>> ListarTodasAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM campania ORDER BY fecha_siembra DESC", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<Campaña>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(Campaña campaña, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO campania
                (codigo, fecha_siembra, cantidad_alevines_sembrados, peso_promedio_inicial_gr, talla_promedio_inicial_cm, proveedor, observaciones)
            VALUES
                (@codigo, @fechaSiembra, @cantidadAlevinesSembrados, @pesoPromedioInicialGr, @tallaPromedioInicialCm, @proveedor, @observaciones)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("codigo", campaña.Codigo);
        command.Parameters.AddWithValue("fechaSiembra", campaña.FechaSiembra);
        command.Parameters.AddWithValue("cantidadAlevinesSembrados", campaña.CantidadAlevinesSembrados);
        command.Parameters.AddWithValue("pesoPromedioInicialGr", campaña.PesoPromedioInicialGr);
        command.Parameters.AddWithValue("tallaPromedioInicialCm", campaña.TallaPromedioInicialCm);
        command.Parameters.AddWithValue("proveedor", (object?)campaña.Proveedor ?? DBNull.Value);
        command.Parameters.AddWithValue("observaciones", (object?)campaña.Observaciones ?? DBNull.Value);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static Campaña Mapear(NpgsqlDataReader reader) => Campaña.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        codigo: reader.GetString(reader.GetOrdinal("codigo")),
        fechaSiembra: reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_siembra")),
        cantidadAlevinesSembrados: reader.GetInt32(reader.GetOrdinal("cantidad_alevines_sembrados")),
        pesoPromedioInicialGr: reader.GetDecimal(reader.GetOrdinal("peso_promedio_inicial_gr")),
        tallaPromedioInicialCm: reader.GetDecimal(reader.GetOrdinal("talla_promedio_inicial_cm")),
        proveedor: reader.GetNullableString("proveedor"),
        observaciones: reader.GetNullableString("observaciones"));
}
