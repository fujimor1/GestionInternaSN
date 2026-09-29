using ErrorOr;
using SierraNevada.Application.Produccion.Repositories;
using SierraNevada.Domain.Produccion;

namespace SierraNevada.Application.Produccion.CasosDeUso;

/// <summary>
/// Orquesta la Selección física por talla (protocolo FONDEPES 8.1.1): separa una porción de
/// un lote hacia otra unidad de producción. Resuelve el escenario de trazabilidad confirmado
/// por el cliente (docs/diseno-modulo-produccion.md sección 5) — el lote hijo queda enlazado
/// al padre y a la misma Campaña.
/// </summary>
public sealed class RealizarSeleccionService(
    ILoteRepository loteRepository,
    IUnidadProduccionRepository unidadProduccionRepository,
    IHistorialMovimientoRepository historialRepository)
{
    public async Task<ErrorOr<int>> EjecutarAsync(
        int lotePadreId,
        int cantidadAMover,
        int nuevaUnidadProduccionId,
        decimal pesoPromedioGr,
        decimal tallaPromedioCm,
        DateOnly fecha,
        CancellationToken ct = default)
    {
        var lotePadre = await loteRepository.ObtenerPorIdAsync(lotePadreId, ct);
        if (lotePadre is null)
            return Error.NotFound("Lote.NoEncontrado", "El lote a seleccionar no existe.");

        var nuevaUnidad = await unidadProduccionRepository.ObtenerPorIdAsync(nuevaUnidadProduccionId, ct);
        if (nuevaUnidad is null)
            return Error.NotFound("UnidadProduccion.NoEncontrada", "La unidad de destino no existe.");

        // Las truchas que van a la cabeza (más grandes) no se pueden mezclar con una jaula de
        // talla muy distinta — mismo problema que documenta FONDEPES sobre no seleccionar:
        // competencia por alimento y mayor mortalidad de los peces menores. Ver
        // docs/diseno-modulo-produccion.md y EtapaProductivaCalculadora.
        var etapaGrupoMovido = EtapaProductivaCalculadora.DesdeTalla(tallaPromedioCm);

        if (!EtapaProductivaCalculadora.TipoUnidadCorresponde(nuevaUnidad, etapaGrupoMovido))
            return Error.Validation(
                "Seleccion.TipoUnidad",
                $"La unidad {nuevaUnidad.Codigo} no es del tipo correcto para peces en etapa {etapaGrupoMovido} (talla {tallaPromedioCm}cm).");

        var lotesEnDestino = (await loteRepository.ListarPorUnidadProduccionAsync(nuevaUnidadProduccionId, ct))
            .Where(l => l.Activo)
            .ToList();

        if (lotesEnDestino.Any(l => l.EtapaActual != etapaGrupoMovido))
            return Error.Validation(
                "Seleccion.TallaIncompatible",
                $"La unidad {nuevaUnidad.Codigo} ya tiene peces en una etapa distinta — no se pueden mezclar tallas muy diferentes en la misma unidad.");

        var biomasaActualEnDestino = lotesEnDestino.Sum(l => l.BiomasaActualKg);
        var biomasaEntrante = cantidadAMover * pesoPromedioGr / 1000m;
        if (biomasaActualEnDestino + biomasaEntrante > nuevaUnidad.CapacidadMaximaKg)
            return Error.Validation(
                "Seleccion.CapacidadExcedida",
                $"La unidad {nuevaUnidad.Codigo} no tiene capacidad suficiente: {biomasaActualEnDestino:F2} kg actuales + {biomasaEntrante:F2} kg entrantes superaría el máximo de {nuevaUnidad.CapacidadMaximaKg:F2} kg.");

        var codigoNuevoLote = await GeneradorCodigoLote.GenerarAsync(loteRepository, fecha, ct);

        var divisionResult = lotePadre.DividirPorSeleccion(
            codigoNuevoLote, cantidadAMover, nuevaUnidadProduccionId, pesoPromedioGr, tallaPromedioCm, fecha);
        if (divisionResult.IsError)
            return divisionResult.Errors;

        var loteHijo = divisionResult.Value;

        // NOTA: sin transacción compartida entre estas escrituras — ver limitación en docs/arquitectura-tecnica.md sección 3.
        await loteRepository.ActualizarAsync(lotePadre, ct);
        var loteHijoId = await loteRepository.CrearAsync(loteHijo, ct);

        var historialPadre = HistorialMovimiento.Crear(
            lotePadreId, fecha.ToDateTime(TimeOnly.MinValue), TipoMovimientoHistorial.Movimiento,
            $"Selección: {cantidadAMover} peces trasladados al lote {codigoNuevoLote}.", cantidadAfectada: cantidadAMover);
        if (!historialPadre.IsError)
            await historialRepository.CrearAsync(historialPadre.Value, ct);

        var historialHijo = HistorialMovimiento.Crear(
            loteHijoId, fecha.ToDateTime(TimeOnly.MinValue), TipoMovimientoHistorial.Creacion,
            $"Lote creado por selección desde {lotePadre.CodigoLote}.", cantidadAfectada: cantidadAMover,
            etapaNueva: loteHijo.EtapaActual);
        if (!historialHijo.IsError)
            await historialRepository.CrearAsync(historialHijo.Value, ct);

        return loteHijoId;
    }
}
