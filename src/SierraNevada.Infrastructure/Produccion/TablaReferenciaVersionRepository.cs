using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class TablaReferenciaVersionRepository(IDbConnectionFactory connectionFactory) : ITablaReferenciaVersionRepository
{
    public async Task<TablaReferenciaVersion?> ObtenerActivaAsync(TipoTablaReferencia tipo, MarcoReferencia marco, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM tabla_referencia_version WHERE tipo = @tipo AND marco = @marco AND activa = TRUE", connection);
        command.Parameters.AddWithValue("tipo", tipo.ToString());
        command.Parameters.AddWithValue("marco", marco.ToString());

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var (id, tipoLeido, marcoLeido, fechaVigencia, fuente, activa) = MapearCabecera(reader);
        await reader.DisposeAsync();

        var valores = await ListarValoresAsync(connection, id, ct);
        return TablaReferenciaVersion.Reconstruir(id, tipoLeido, marcoLeido, fechaVigencia, fuente, activa, valores);
    }

    public async Task<TablaReferenciaVersion?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM tabla_referencia_version WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var (versionId, tipo, marco, fechaVigencia, fuente, activa) = MapearCabecera(reader);
        await reader.DisposeAsync();

        var valores = await ListarValoresAsync(connection, versionId, ct);
        return TablaReferenciaVersion.Reconstruir(versionId, tipo, marco, fechaVigencia, fuente, activa, valores);
    }

    public async Task<IReadOnlyList<TablaReferenciaVersion>> ListarPorTipoAsync(TipoTablaReferencia tipo, MarcoReferencia marco, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM tabla_referencia_version WHERE tipo = @tipo AND marco = @marco ORDER BY fecha_vigencia_desde DESC", connection);
        command.Parameters.AddWithValue("tipo", tipo.ToString());
        command.Parameters.AddWithValue("marco", marco.ToString());

        var cabeceras = new List<(int Id, TipoTablaReferencia Tipo, MarcoReferencia Marco, DateOnly FechaVigencia, string Fuente, bool Activa)>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                cabeceras.Add(MapearCabecera(reader));
        }

        var resultado = new List<TablaReferenciaVersion>();
        foreach (var (id, tipoLeido, marcoLeido, fechaVigencia, fuente, activa) in cabeceras)
        {
            var valores = await ListarValoresAsync(connection, id, ct);
            resultado.Add(TablaReferenciaVersion.Reconstruir(id, tipoLeido, marcoLeido, fechaVigencia, fuente, activa, valores));
        }

        return resultado;
    }

    /// <summary>Crea la versión + valores y la activa, desactivando la anterior DEL MISMO MARCO — todo en una transacción.</summary>
    public async Task<int> CrearYActivarAsync(TablaReferenciaVersion version, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await using (var desactivar = new NpgsqlCommand(
            "UPDATE tabla_referencia_version SET activa = FALSE WHERE tipo = @tipo AND marco = @marco AND activa = TRUE", connection, transaction))
        {
            desactivar.Parameters.AddWithValue("tipo", version.Tipo.ToString());
            desactivar.Parameters.AddWithValue("marco", version.Marco.ToString());
            await desactivar.ExecuteNonQueryAsync(ct);
        }

        int versionId;
        await using (var crear = new NpgsqlCommand("""
            INSERT INTO tabla_referencia_version (tipo, marco, fecha_vigencia_desde, fuente, activa)
            VALUES (@tipo, @marco, @fechaVigenciaDesde, @fuente, TRUE)
            RETURNING id
            """, connection, transaction))
        {
            crear.Parameters.AddWithValue("tipo", version.Tipo.ToString());
            crear.Parameters.AddWithValue("marco", version.Marco.ToString());
            crear.Parameters.AddWithValue("fechaVigenciaDesde", version.FechaVigenciaDesde);
            crear.Parameters.AddWithValue("fuente", version.Fuente);
            versionId = (int)(await crear.ExecuteScalarAsync(ct))!;
        }

        foreach (var valor in version.Valores)
        {
            await using var crearValor = new NpgsqlCommand("""
                INSERT INTO tabla_referencia_valor
                    (version_id, talla_min_cm, talla_max_cm, temperatura_min_c, temperatura_max_c, valor)
                VALUES
                    (@versionId, @tallaMinCm, @tallaMaxCm, @temperaturaMinC, @temperaturaMaxC, @valor)
                """, connection, transaction);

            crearValor.Parameters.AddWithValue("versionId", versionId);
            crearValor.Parameters.AddWithValue("tallaMinCm", valor.TallaMinCm);
            crearValor.Parameters.AddWithValue("tallaMaxCm", valor.TallaMaxCm);
            crearValor.Parameters.AddWithValue("temperaturaMinC", (object?)valor.TemperaturaMinC ?? DBNull.Value);
            crearValor.Parameters.AddWithValue("temperaturaMaxC", (object?)valor.TemperaturaMaxC ?? DBNull.Value);
            crearValor.Parameters.AddWithValue("valor", valor.Valor);

            await crearValor.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
        return versionId;
    }

    private static async Task<List<TablaReferenciaValor>> ListarValoresAsync(NpgsqlConnection connection, int versionId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tabla_referencia_valor WHERE version_id = @versionId ORDER BY talla_min_cm", connection);
        command.Parameters.AddWithValue("versionId", versionId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<TablaReferenciaValor>();
        while (await reader.ReadAsync(ct))
        {
            resultado.Add(TablaReferenciaValor.Reconstruir(
                id: reader.GetInt32(reader.GetOrdinal("id")),
                versionId: reader.GetInt32(reader.GetOrdinal("version_id")),
                tallaMinCm: reader.GetDecimal(reader.GetOrdinal("talla_min_cm")),
                tallaMaxCm: reader.GetDecimal(reader.GetOrdinal("talla_max_cm")),
                temperaturaMinC: reader.GetNullableDecimal("temperatura_min_c"),
                temperaturaMaxC: reader.GetNullableDecimal("temperatura_max_c"),
                valor: reader.GetDecimal(reader.GetOrdinal("valor"))));
        }

        return resultado;
    }

    private static (int Id, TipoTablaReferencia Tipo, MarcoReferencia Marco, DateOnly FechaVigencia, string Fuente, bool Activa) MapearCabecera(NpgsqlDataReader reader) => (
        reader.GetInt32(reader.GetOrdinal("id")),
        Enum.Parse<TipoTablaReferencia>(reader.GetString(reader.GetOrdinal("tipo"))),
        Enum.Parse<MarcoReferencia>(reader.GetString(reader.GetOrdinal("marco"))),
        reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_vigencia_desde")),
        reader.GetString(reader.GetOrdinal("fuente")),
        reader.GetBoolean(reader.GetOrdinal("activa")));
}
