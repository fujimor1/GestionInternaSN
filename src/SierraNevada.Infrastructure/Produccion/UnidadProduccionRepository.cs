using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class UnidadProduccionRepository(IDbConnectionFactory connectionFactory) : IUnidadProduccionRepository
{
    public async Task<UnidadProduccion?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM unidad_produccion WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<UnidadProduccion?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM unidad_produccion WHERE codigo = @codigo", connection);
        command.Parameters.AddWithValue("codigo", codigo);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<IReadOnlyList<UnidadProduccion>> ListarTodasAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM unidad_produccion ORDER BY codigo", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<UnidadProduccion>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(UnidadProduccion unidad, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO unidad_produccion
                (codigo, tipo, sub_tipo_jaula, forma, largo_m, ancho_m, diametro_m, lado_m, alto_m, densidad_siembra_kg_m3)
            VALUES
                (@codigo, @tipo, @subTipoJaula, @forma, @largoM, @anchoM, @diametroM, @ladoM, @altoM, @densidadSiembraKgM3)
            RETURNING id
            """, connection);

        AgregarParametros(command, unidad);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task ActualizarAsync(UnidadProduccion unidad, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            UPDATE unidad_produccion SET
                codigo = @codigo, tipo = @tipo, sub_tipo_jaula = @subTipoJaula, forma = @forma,
                largo_m = @largoM, ancho_m = @anchoM, diametro_m = @diametroM, lado_m = @ladoM,
                alto_m = @altoM, densidad_siembra_kg_m3 = @densidadSiembraKgM3
            WHERE id = @id
            """, connection);

        AgregarParametros(command, unidad);
        command.Parameters.AddWithValue("id", unidad.Id);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AgregarParametros(NpgsqlCommand command, UnidadProduccion unidad)
    {
        command.Parameters.AddWithValue("codigo", unidad.Codigo);
        command.Parameters.AddWithValue("tipo", unidad.Tipo.ToString());
        command.Parameters.AddWithValue("subTipoJaula", (object?)unidad.SubTipoJaula?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("forma", unidad.Forma.ToString());
        command.Parameters.AddWithValue("largoM", (object?)unidad.LargoM ?? DBNull.Value);
        command.Parameters.AddWithValue("anchoM", (object?)unidad.AnchoM ?? DBNull.Value);
        command.Parameters.AddWithValue("diametroM", (object?)unidad.DiametroM ?? DBNull.Value);
        command.Parameters.AddWithValue("ladoM", (object?)unidad.LadoM ?? DBNull.Value);
        command.Parameters.AddWithValue("altoM", unidad.AltoM);
        command.Parameters.AddWithValue("densidadSiembraKgM3", unidad.DensidadSiembraKgM3);
    }

    private static UnidadProduccion Mapear(NpgsqlDataReader reader) => UnidadProduccion.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        codigo: reader.GetString(reader.GetOrdinal("codigo")),
        tipo: Enum.Parse<TipoUnidadProduccion>(reader.GetString(reader.GetOrdinal("tipo"))),
        subTipoJaula: reader.GetNullableEnum<TipoJaula>("sub_tipo_jaula"),
        forma: Enum.Parse<FormaUnidad>(reader.GetString(reader.GetOrdinal("forma"))),
        largoM: reader.GetNullableDecimal("largo_m"),
        anchoM: reader.GetNullableDecimal("ancho_m"),
        diametroM: reader.GetNullableDecimal("diametro_m"),
        // lado_m ya no se lee — LadoM se recalcula siempre desde diametroM (propiedad computada).
        altoM: reader.GetDecimal(reader.GetOrdinal("alto_m")),
        densidadSiembraKgM3: reader.GetDecimal(reader.GetOrdinal("densidad_siembra_kg_m3")));
}
