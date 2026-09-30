using Npgsql;
using SierraNevada.Application.Inventario.Repositories;
using SierraNevada.Domain.Inventario;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Inventario;

public sealed class ProveedorAlimentoRepository(IDbConnectionFactory connectionFactory) : IProveedorAlimentoRepository
{
    public async Task<IReadOnlyList<ProveedorAlimento>> ObtenerTodosAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT * FROM proveedor_alimento WHERE activo = TRUE ORDER BY razon_social", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var resultado = new List<ProveedorAlimento>();
        while (await reader.ReadAsync(ct))
            resultado.Add(Mapear(reader));

        return resultado;
    }

    public async Task<ProveedorAlimento?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM proveedor_alimento WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<int> CrearAsync(ProveedorAlimento proveedor, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO proveedor_alimento (
                ruc, razon_social, contacto_nombre, telefono, email,
                lead_time_dias_promedio, costo_orden_pedido, activo, fecha_creacion
            )
            VALUES (
                @ruc, @razonSocial, @contactoNombre, @telefono, @email,
                @leadTimeDiasPromedio, @costoOrdenPedido, @activo, @fechaCreacion
            )
            RETURNING id
            """, connection);

        command.Parameters.AddWithValue("ruc", proveedor.Ruc);
        command.Parameters.AddWithValue("razonSocial", proveedor.RazonSocial);
        command.Parameters.AddWithValue("contactoNombre", (object?)proveedor.ContactoNombre ?? DBNull.Value);
        command.Parameters.AddWithValue("telefono", (object?)proveedor.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("email", (object?)proveedor.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("leadTimeDiasPromedio", proveedor.LeadTimeDiasPromedio);
        command.Parameters.AddWithValue("costoOrdenPedido", proveedor.CostoOrdenPedido);
        command.Parameters.AddWithValue("activo", proveedor.Activo);
        command.Parameters.AddWithValue("fechaCreacion", proveedor.FechaCreacion);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task ActualizarAsync(ProveedorAlimento proveedor, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            UPDATE proveedor_alimento SET
                ruc = @ruc,
                razon_social = @razonSocial,
                contacto_nombre = @contactoNombre,
                telefono = @telefono,
                email = @email,
                lead_time_dias_promedio = @leadTimeDiasPromedio,
                costo_orden_pedido = @costoOrdenPedido,
                activo = @activo
            WHERE id = @id
            """, connection);

        command.Parameters.AddWithValue("id", proveedor.Id);
        command.Parameters.AddWithValue("ruc", proveedor.Ruc);
        command.Parameters.AddWithValue("razonSocial", proveedor.RazonSocial);
        command.Parameters.AddWithValue("contactoNombre", (object?)proveedor.ContactoNombre ?? DBNull.Value);
        command.Parameters.AddWithValue("telefono", (object?)proveedor.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("email", (object?)proveedor.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("leadTimeDiasPromedio", proveedor.LeadTimeDiasPromedio);
        command.Parameters.AddWithValue("costoOrdenPedido", proveedor.CostoOrdenPedido);
        command.Parameters.AddWithValue("activo", proveedor.Activo);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static ProveedorAlimento Mapear(NpgsqlDataReader reader) => ProveedorAlimento.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        ruc: reader.GetString(reader.GetOrdinal("ruc")),
        razonSocial: reader.GetString(reader.GetOrdinal("razon_social")),
        contactoNombre: reader.GetNullableString("contacto_nombre"),
        telefono: reader.GetNullableString("telefono"),
        email: reader.GetNullableString("email"),
        leadTimeDiasPromedio: reader.GetInt32(reader.GetOrdinal("lead_time_dias_promedio")),
        costoOrdenPedido: reader.GetDecimal(reader.GetOrdinal("costo_orden_pedido")),
        activo: reader.GetBoolean(reader.GetOrdinal("activo")),
        fechaCreacion: reader.GetDateTime(reader.GetOrdinal("fecha_creacion")));
}
