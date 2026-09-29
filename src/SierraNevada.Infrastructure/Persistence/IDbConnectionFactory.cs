using Npgsql;

namespace SierraNevada.Infrastructure.Persistence;

/// <summary>Crea conexiones ADO.NET a PostgreSQL. Uso interno de Infrastructure — Application
/// nunca conoce Npgsql, solo las interfaces de repositorio (así la decisión ADO.NET es reversible,
/// ver docs/arquitectura-tecnica.md sección 3).</summary>
public interface IDbConnectionFactory
{
    NpgsqlConnection CreateConnection();
}

public sealed class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public NpgsqlConnection CreateConnection() => new(connectionString);
}
