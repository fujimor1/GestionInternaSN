using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class BastidorRepository(IDbConnectionFactory connectionFactory) : IBastidorRepository
{
    public async Task<Bastidor?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM bastidor WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<IReadOnlyList<Bastidor>> ListarDisponiblesAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM bastidor WHERE esta_disponible = TRUE ORDER BY codigo", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<Bastidor>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(Bastidor bastidor, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO bastidor (codigo, capacidad_maxima_unidades, esta_disponible)
            VALUES (@codigo, @capacidadMaximaUnidades, @estaDisponible)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("codigo", bastidor.Codigo);
        command.Parameters.AddWithValue("capacidadMaximaUnidades", bastidor.CapacidadMaximaUnidades);
        command.Parameters.AddWithValue("estaDisponible", bastidor.EstaDisponible);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task ActualizarAsync(Bastidor bastidor, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            UPDATE bastidor SET codigo = @codigo, capacidad_maxima_unidades = @capacidadMaximaUnidades, esta_disponible = @estaDisponible
            WHERE id = @id
            """, connection);

        command.Parameters.AddWithValue("codigo", bastidor.Codigo);
        command.Parameters.AddWithValue("capacidadMaximaUnidades", bastidor.CapacidadMaximaUnidades);
        command.Parameters.AddWithValue("estaDisponible", bastidor.EstaDisponible);
        command.Parameters.AddWithValue("id", bastidor.Id);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static Bastidor Mapear(NpgsqlDataReader reader) => Bastidor.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        codigo: reader.GetString(reader.GetOrdinal("codigo")),
        capacidadMaximaUnidades: reader.GetInt32(reader.GetOrdinal("capacidad_maxima_unidades")),
        estaDisponible: reader.GetBoolean(reader.GetOrdinal("esta_disponible")));
}
