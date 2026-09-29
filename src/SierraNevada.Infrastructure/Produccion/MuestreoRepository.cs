using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class MuestreoRepository(IDbConnectionFactory connectionFactory) : IMuestreoRepository
{
    public async Task<Muestreo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM muestreo WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<IReadOnlyList<Muestreo>> ListarPorLoteAsync(int loteId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM muestreo WHERE lote_id = @loteId ORDER BY fecha ASC", connection);
        command.Parameters.AddWithValue("loteId", loteId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<Muestreo>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<Muestreo?> ObtenerUltimoPorLoteAsync(int loteId, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM muestreo WHERE lote_id = @loteId ORDER BY fecha DESC LIMIT 1", connection);
        command.Parameters.AddWithValue("loteId", loteId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<int> CrearAsync(Muestreo muestreo, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO muestreo
                (lote_id, fecha, peso_promedio_muestreado_gr, talla_promedio_muestreada_cm,
                 numero_peces_muestreados, cantidad_peces_vivos_al_momento, registrado_por)
            VALUES
                (@loteId, @fecha, @pesoPromedioMuestreadoGr, @tallaPromedioMuestreadaCm,
                 @numeroPecesMuestreados, @cantidadPecesVivosAlMomento, @registradoPor)
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("loteId", muestreo.LoteId);
        command.Parameters.AddWithValue("fecha", muestreo.Fecha);
        command.Parameters.AddWithValue("pesoPromedioMuestreadoGr", muestreo.PesoPromedioMuestreadoGr);
        command.Parameters.AddWithValue("tallaPromedioMuestreadaCm", muestreo.TallaPromedioMuestreadaCm);
        command.Parameters.AddWithValue("numeroPecesMuestreados", muestreo.NumeroPecesMuestreados);
        command.Parameters.AddWithValue("cantidadPecesVivosAlMomento", muestreo.CantidadPecesVivosAlMomento);
        command.Parameters.AddWithValue("registradoPor", muestreo.RegistradoPor);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static Muestreo Mapear(NpgsqlDataReader reader) => Muestreo.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        loteId: reader.GetInt32(reader.GetOrdinal("lote_id")),
        fecha: reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha")),
        pesoPromedioMuestreadoGr: reader.GetDecimal(reader.GetOrdinal("peso_promedio_muestreado_gr")),
        tallaPromedioMuestreadaCm: reader.GetDecimal(reader.GetOrdinal("talla_promedio_muestreada_cm")),
        numeroPecesMuestreados: reader.GetInt32(reader.GetOrdinal("numero_peces_muestreados")),
        cantidadPecesVivosAlMomento: reader.GetInt32(reader.GetOrdinal("cantidad_peces_vivos_al_momento")),
        registradoPor: reader.GetString(reader.GetOrdinal("registrado_por")));
}
