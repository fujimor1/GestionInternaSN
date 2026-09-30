using Npgsql;
using SierraNevada.Application.Inventario.Repositories;
using SierraNevada.Domain.Inventario;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Inventario;

public sealed class TipoAlimentoRepository(IDbConnectionFactory connectionFactory) : ITipoAlimentoRepository
{
    public async Task<IReadOnlyList<TipoAlimentoCatalogo>> ObtenerTodosAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM tipo_alimento WHERE activo = TRUE ORDER BY calibre_mm, nombre", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<TipoAlimentoCatalogo>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<TipoAlimentoCatalogo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM tipo_alimento WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<int> CrearAsync(TipoAlimentoCatalogo tipoAlimento, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO tipo_alimento (
                nombre, marca, calibre_mm, porcentaje_proteina, porcentaje_grasa,
                etapa_sugerida, costo_unitario_promedio_kg, costo_almacenamiento_anual_por_kg, activo, fecha_creacion
            )
            VALUES (
                @nombre, @marca, @calibreMm, @porcentajeProteina, @porcentajeGrasa,
                @etapaSugerida, @costoUnitarioPromedioKg, @costoAlmacenamientoAnualPorKg, @activo, @fechaCreacion
            )
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("nombre", tipoAlimento.Nombre);
        command.Parameters.AddWithValue("marca", tipoAlimento.Marca);
        command.Parameters.AddWithValue("calibreMm", tipoAlimento.CalibreMm);
        command.Parameters.AddWithValue("porcentajeProteina", tipoAlimento.PorcentajeProteina);
        command.Parameters.AddWithValue("porcentajeGrasa", tipoAlimento.PorcentajeGrasa);
        command.Parameters.AddWithValue("etapaSugerida", tipoAlimento.EtapaSugerida);
        command.Parameters.AddWithValue("costoUnitarioPromedioKg", tipoAlimento.CostoUnitarioPromedioKg);
        command.Parameters.AddWithValue("costoAlmacenamientoAnualPorKg", tipoAlimento.CostoAlmacenamientoAnualPorKg);
        command.Parameters.AddWithValue("activo", tipoAlimento.Activo);
        command.Parameters.AddWithValue("fechaCreacion", tipoAlimento.FechaCreacion);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task ActualizarAsync(TipoAlimentoCatalogo tipoAlimento, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            UPDATE tipo_alimento SET
                nombre = @nombre,
                marca = @marca,
                calibre_mm = @calibreMm,
                porcentaje_proteina = @porcentajeProteina,
                porcentaje_grasa = @porcentajeGrasa,
                etapa_sugerida = @etapaSugerida,
                costo_unitario_promedio_kg = @costoUnitarioPromedioKg,
                costo_almacenamiento_anual_por_kg = @costoAlmacenamientoAnualPorKg,
                activo = @activo
            WHERE id = @id
            """, connection);

        command.Parameters.AddWithValue("id", tipoAlimento.Id);
        command.Parameters.AddWithValue("nombre", tipoAlimento.Nombre);
        command.Parameters.AddWithValue("marca", tipoAlimento.Marca);
        command.Parameters.AddWithValue("calibreMm", tipoAlimento.CalibreMm);
        command.Parameters.AddWithValue("porcentajeProteina", tipoAlimento.PorcentajeProteina);
        command.Parameters.AddWithValue("porcentajeGrasa", tipoAlimento.PorcentajeGrasa);
        command.Parameters.AddWithValue("etapaSugerida", tipoAlimento.EtapaSugerida);
        command.Parameters.AddWithValue("costoUnitarioPromedioKg", tipoAlimento.CostoUnitarioPromedioKg);
        command.Parameters.AddWithValue("costoAlmacenamientoAnualPorKg", tipoAlimento.CostoAlmacenamientoAnualPorKg);
        command.Parameters.AddWithValue("activo", tipoAlimento.Activo);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static TipoAlimentoCatalogo Mapear(NpgsqlDataReader reader) => TipoAlimentoCatalogo.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        nombre: reader.GetString(reader.GetOrdinal("nombre")),
        marca: reader.GetString(reader.GetOrdinal("marca")),
        calibreMm: reader.GetDecimal(reader.GetOrdinal("calibre_mm")),
        porcentajeProteina: reader.GetDecimal(reader.GetOrdinal("porcentaje_proteina")),
        porcentajeGrasa: reader.GetDecimal(reader.GetOrdinal("porcentaje_grasa")),
        etapaSugerida: reader.GetString(reader.GetOrdinal("etapa_sugerida")),
        costoUnitarioPromedioKg: reader.GetDecimal(reader.GetOrdinal("costo_unitario_promedio_kg")),
        costoAlmacenamientoAnualPorKg: reader.GetDecimal(reader.GetOrdinal("costo_almacenamiento_anual_por_kg")),
        activo: reader.GetBoolean(reader.GetOrdinal("activo")),
        fechaCreacion: reader.GetDateTime(reader.GetOrdinal("fecha_creacion")));
}
