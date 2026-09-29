using SierraNevada.Application.Produccion.Repositories;

namespace SierraNevada.Application.Produccion.CasosDeUso;

/// <summary>
/// El código de lote ya no lo escribe el operario (chocaba con el conteo automático de peces
/// vivos por el mismo motivo: un dato que debería ser predecible por el sistema, no adivinado
/// por una persona). Sigue la misma convención que ya usan bastidores/artesas/jaulas del
/// inventario real (año+mes de 2 dígitos + secuencial), para que el código siga siendo legible:
/// L{AAMM}-{NN}, por ejemplo L2609-01.
/// </summary>
internal static class GeneradorCodigoLote
{
    public static async Task<string> GenerarAsync(ILoteRepository loteRepository, DateOnly fecha, CancellationToken ct)
    {
        var prefijo = $"L{fecha:yyMM}-";
        var siguiente = await loteRepository.ContarPorPrefijoCodigoAsync(prefijo, ct) + 1;
        var codigo = $"{prefijo}{siguiente:D2}";

        // Re-chequeo por si el conteo y un código ya usado no coinciden (p. ej. lotes borrados a mano).
        while (await loteRepository.ObtenerPorCodigoAsync(codigo, ct) is not null)
        {
            siguiente++;
            codigo = $"{prefijo}{siguiente:D2}";
        }

        return codigo;
    }
}
