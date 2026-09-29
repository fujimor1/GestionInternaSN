using Npgsql;

namespace SierraNevada.Infrastructure.Persistence;

/// <summary>Helpers de lectura para columnas nulas — sigue siendo ADO.NET puro, solo evita repetir reader.IsDBNull(...) en cada campo opcional.</summary>
internal static class DataReaderExtensions
{
    public static decimal? GetNullableDecimal(this NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    public static int? GetNullableInt(this NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    public static string? GetNullableString(this NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static DateOnly? GetNullableDateOnly(this NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateOnly>(ordinal);
    }

    public static TEnum? GetNullableEnum<TEnum>(this NpgsqlDataReader reader, string column) where TEnum : struct, Enum
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Enum.Parse<TEnum>(reader.GetString(ordinal));
    }
}
