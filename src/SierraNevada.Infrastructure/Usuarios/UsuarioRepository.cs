using Npgsql;
using SierraNevada.Application.Usuarios.Repositories;
using SierraNevada.Domain.Usuarios;
using SierraNevada.Infrastructure.Persistence;

namespace SierraNevada.Infrastructure.Usuarios;

public sealed class UsuarioRepository(IDbConnectionFactory connectionFactory) : IUsuarioRepository
{
    public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM usuario WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT * FROM usuario WHERE nombre_usuario = @nombreUsuario", connection);
        command.Parameters.AddWithValue("nombreUsuario", nombreUsuario);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<bool> ExisteAlgunUsuarioAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM usuario)", connection);
        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<int> CrearAsync(Usuario usuario, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            INSERT INTO usuario (nombre_usuario, email, password_hash, nombre_completo, rol, activo, fecha_creacion)
            VALUES (@nombreUsuario, @email, @passwordHash, @nombreCompleto, @rol, @activo, @fechaCreacion)
            RETURNING id
            """, connection);

        AgregarParametros(command, usuario);

        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task ActualizarAsync(Usuario usuario, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand("""
            UPDATE usuario SET
                nombre_usuario = @nombreUsuario, email = @email, password_hash = @passwordHash,
                nombre_completo = @nombreCompleto, rol = @rol, activo = @activo
            WHERE id = @id
            """, connection);

        AgregarParametros(command, usuario);
        command.Parameters.AddWithValue("id", usuario.Id);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AgregarParametros(NpgsqlCommand command, Usuario usuario)
    {
        command.Parameters.AddWithValue("nombreUsuario", usuario.NombreUsuario);
        command.Parameters.AddWithValue("email", usuario.Email);
        command.Parameters.AddWithValue("passwordHash", usuario.PasswordHash);
        command.Parameters.AddWithValue("nombreCompleto", usuario.NombreCompleto);
        command.Parameters.AddWithValue("rol", usuario.Rol.ToString());
        command.Parameters.AddWithValue("activo", usuario.Activo);
        command.Parameters.AddWithValue("fechaCreacion", usuario.FechaCreacion);
    }

    private static Usuario Mapear(NpgsqlDataReader reader) => Usuario.Reconstruir(
        id: reader.GetInt32(reader.GetOrdinal("id")),
        nombreUsuario: reader.GetString(reader.GetOrdinal("nombre_usuario")),
        email: reader.GetString(reader.GetOrdinal("email")),
        passwordHash: reader.GetString(reader.GetOrdinal("password_hash")),
        nombreCompleto: reader.GetString(reader.GetOrdinal("nombre_completo")),
        rol: Enum.Parse<RolUsuario>(reader.GetString(reader.GetOrdinal("rol"))),
        activo: reader.GetBoolean(reader.GetOrdinal("activo")),
        fechaCreacion: reader.GetDateTime(reader.GetOrdinal("fecha_creacion")));
}
