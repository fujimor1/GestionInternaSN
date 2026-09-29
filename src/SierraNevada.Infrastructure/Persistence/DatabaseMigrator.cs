using DbUp;

namespace SierraNevada.Infrastructure.Persistence;

/// <summary>
/// Reemplaza las migraciones automáticas de EF Core: aplica y versiona los scripts .sql de
/// Persistence/Scripts en orden (por nombre), una sola vez cada uno. Se llama al iniciar la
/// API (Program.cs) — ver docs/arquitectura-tecnica.md sección 3.
/// </summary>
public static class DatabaseMigrator
{
    public static void MigrateDatabase(string connectionString)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new InvalidOperationException("Error al migrar la base de datos. Ver log para detalle.", result.Error);
    }
}
