using Npgsql;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Produccion;

public sealed class LoteRepository(IDbConnectionFactory connectionFactory) : ILoteRepository
{
    public async Task<Lote?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM lote WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<Lote?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM lote WHERE codigo_lote = @codigo", connection);
        command.Parameters.AddWithValue("codigo", codigo);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<int> ContarPorPrefijoCodigoAsync(string prefijo, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM lote WHERE codigo_lote LIKE @prefijo || '%'", connection);
        command.Parameters.AddWithValue("prefijo", prefijo);

        return (int)(long)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<IReadOnlyList<Lote>> ListarPorCampañaAsync(int campañaId, CancellationToken ct = default)
        => await ListarConFiltroAsync("campania_id = @filtro", campañaId, ct);

    public async Task<IReadOnlyList<Lote>> ListarHijosAsync(int lotePadreId, CancellationToken ct = default)
        => await ListarConFiltroAsync("lote_padre_id = @filtro", lotePadreId, ct);

    public async Task<IReadOnlyList<Lote>> ListarPorUnidadProduccionAsync(int unidadProduccionId, CancellationToken ct = default)
        => await ListarConFiltroAsync("unidad_produccion_id = @filtro", unidadProduccionId, ct);

    public async Task<IReadOnlyList<Lote>> ListarActivosAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM lote WHERE activo = TRUE ORDER BY fecha_ingreso_etapa DESC", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<Lote>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    private async Task<IReadOnlyList<Lote>> ListarConFiltroAsync(string whereClause, int filtro, CancellationToken ct)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            $"SELECT * FROM lote WHERE {whereClause} ORDER BY fecha_ingreso_etapa DESC", connection);
        command.Parameters.AddWithValue("filtro", filtro);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<Lote>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<int> CrearAsync(Lote lote, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO lote
                (codigo_lote, campania_id, lote_padre_id, unidad_produccion_id, bastidor_id, etapa_actual,
                 cantidad_inicial, peso_promedio_inicial_gr, cantidad_total_peces, talla_promedio_actual_cm,
                 peso_promedio_actual_gr, fecha_ingreso_etapa, fecha_fin, activo)
            VALUES
                (@codigoLote, @campañaId, @lotePadreId, @unidadProduccionId, @bastidorId, @etapaActual,
                 @cantidadInicial, @pesoPromedioInicialGr, @cantidadTotalPeces, @tallaPromedioActualCm,
                 @pesoPromedioActualGr, @fechaIngresoEtapa, @fechaFin, @activo)
            RETURNING id
            """, connection);

        AgregarParametros(command, lote);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task ActualizarAsync(Lote lote, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            UPDATE lote SET
                codigo_lote = @codigoLote, campania_id = @campañaId, lote_padre_id = @lotePadreId,
                unidad_produccion_id = @unidadProduccionId, bastidor_id = @bastidorId, etapa_actual = @etapaActual,
                cantidad_inicial = @cantidadInicial, peso_promedio_inicial_gr = @pesoPromedioInicialGr,
                cantidad_total_peces = @cantidadTotalPeces, talla_promedio_actual_cm = @tallaPromedioActualCm,
                peso_promedio_actual_gr = @pesoPromedioActualGr, fecha_ingreso_etapa = @fechaIngresoEtapa,
                fecha_fin = @fechaFin, activo = @activo
            WHERE id = @id
            """, connection);

        AgregarParametros(command, lote);
        command.Parameters.AddWithValue("id", lote.Id);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AgregarParametros(NpgsqlCommand command, Lote lote)
    {
        command.Parameters.AddWithValue("codigoLote", lote.CodigoLote);
        command.Parameters.AddWithValue("campañaId", lote.CampañaId);
        command.Parameters.AddWithValue("lotePadreId", (object?)lote.LotePadreId ?? DBNull.Value);
        command.Parameters.AddWithValue("unidadProduccionId", (object?)lote.UnidadProduccionId ?? DBNull.Value);
        command.Parameters.AddWithValue("bastidorId", (object?)lote.BastidorId ?? DBNull.Value);
        command.Parameters.AddWithValue("etapaActual", lote.EtapaActual.ToString());
        command.Parameters.AddWithValue("cantidadInicial", lote.CantidadInicial);
        command.Parameters.AddWithValue("pesoPromedioInicialGr", lote.PesoPromedioInicialGr);
        command.Parameters.AddWithValue("cantidadTotalPeces", lote.CantidadTotalPeces);
        command.Parameters.AddWithValue("tallaPromedioActualCm", (object?)lote.TallaPromedioActualCm ?? DBNull.Value);
        command.Parameters.AddWithValue("pesoPromedioActualGr", (object?)lote.PesoPromedioActualGr ?? DBNull.Value);
        command.Parameters.AddWithValue("fechaIngresoEtapa", lote.FechaIngresoEtapa);
        command.Parameters.AddWithValue("fechaFin", (object?)lote.FechaFin ?? DBNull.Value);
        command.Parameters.AddWithValue("activo", lote.Activo);
    }

    private static Lote Mapear(NpgsqlDataReader reader) => Lote.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        codigoLote: reader.GetString(reader.GetOrdinal("codigo_lote")),
        campañaId: reader.GetInt32(reader.GetOrdinal("campania_id")),
        lotePadreId: reader.GetNullableInt("lote_padre_id"),
        unidadProduccionId: reader.GetNullableInt("unidad_produccion_id"),
        bastidorId: reader.GetNullableInt("bastidor_id"),
        etapaActual: Enum.Parse<EtapaProductiva>(reader.GetString(reader.GetOrdinal("etapa_actual"))),
        cantidadInicial: reader.GetInt32(reader.GetOrdinal("cantidad_inicial")),
        pesoPromedioInicialGr: reader.GetDecimal(reader.GetOrdinal("peso_promedio_inicial_gr")),
        cantidadTotalPeces: reader.GetInt32(reader.GetOrdinal("cantidad_total_peces")),
        tallaPromedioActualCm: reader.GetNullableDecimal("talla_promedio_actual_cm"),
        pesoPromedioActualGr: reader.GetNullableDecimal("peso_promedio_actual_gr"),
        fechaIngresoEtapa: reader.GetFieldValue<DateOnly>(reader.GetOrdinal("fecha_ingreso_etapa")),
        fechaFin: reader.GetNullableDateOnly("fecha_fin"),
        activo: reader.GetBoolean(reader.GetOrdinal("activo")));
}
