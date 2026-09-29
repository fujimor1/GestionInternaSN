using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class EnfermedadRepository(IDbConnectionFactory connectionFactory) : IEnfermedadRepository
{
    public async Task<Enfermedad?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM enfermedad WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<IReadOnlyList<Enfermedad>> ListarTodasAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM enfermedad ORDER BY nombre", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<Enfermedad>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(Enfermedad enfermedad, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO enfermedad (nombre, descripcion, tratamiento, prevencion)
            VALUES (@nombre, @descripcion, @tratamiento, @prevencion)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("nombre", enfermedad.Nombre);
        command.Parameters.AddWithValue("descripcion", enfermedad.Descripcion);
        command.Parameters.AddWithValue("tratamiento", enfermedad.Tratamiento);
        command.Parameters.AddWithValue("prevencion", enfermedad.Prevencion);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static Enfermedad Mapear(NpgsqlDataReader reader) => Enfermedad.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        nombre: reader.GetString(reader.GetOrdinal("nombre")),
        descripcion: reader.GetString(reader.GetOrdinal("descripcion")),
        tratamiento: reader.GetString(reader.GetOrdinal("tratamiento")),
        prevencion: reader.GetString(reader.GetOrdinal("prevencion")));
}
