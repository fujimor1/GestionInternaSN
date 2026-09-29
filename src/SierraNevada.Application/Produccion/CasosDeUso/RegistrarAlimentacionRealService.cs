using ErrorOr;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.CasosDeUso;

/// <summary>Registra alimento real entregado (kg) — resuelve el gap #2: sin esto el FCA real nunca se puede calcular.</summary>
public sealed class RegistrarAlimentacionRealService(
    ILoteRepository loteRepository,
    IRegistroAlimentacionRealRepository registroRepository)
{
    public async Task<ErrorOr<int>> EjecutarAsync(
        int loteId, DateOnly fecha, decimal cantidadKgEntregada, TipoAlimento tipoAlimento, CancellationToken ct = default)
    {
        var lote = await loteRepository.ObtenerPorIdAsync(loteId, ct);
        if (lote is null)
            return Error.NotFound("Lote.NoEncontrado", "El lote no existe.");

        var registroResult = RegistroAlimentacionReal.Crear(loteId, fecha, cantidadKgEntregada, tipoAlimento);
        if (registroResult.IsError)
            return registroResult.Errors;

        return await registroRepository.CrearAsync(registroResult.Value, ct);
    }
}
