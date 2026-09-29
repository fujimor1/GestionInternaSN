using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Evento real de pesaje/medición (protocolo FONDEPES "Inventario", sección 8.1 del PDF fuente
/// en docs/fuentes/). Es la pieza central que el sistema Django anterior no tenía: preserva la
/// serie de tiempo de peso/talla por lote en vez de sobrescribirla (docs/diseno-modulo-produccion.md, 4.3).
/// </summary>
public sealed class Muestreo
{
    public int Id { get; private set; }
    public int LoteId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public decimal PesoPromedioMuestreadoGr { get; private set; }
    public decimal TallaPromedioMuestreadaCm { get; private set; }
    public int NumeroPecesMuestreados { get; private set; }
    public int CantidadPecesVivosAlMomento { get; private set; }
    public string RegistradoPor { get; private set; } = string.Empty;

    private Muestreo() { }

    public static ErrorOr<Muestreo> Crear(
        int loteId,
        DateOnly fecha,
        decimal pesoPromedioMuestreadoGr,
        decimal tallaPromedioMuestreadaCm,
        int numeroPecesMuestreados,
        int cantidadPecesVivosAlMomento,
        string registradoPor)
    {
        if (pesoPromedioMuestreadoGr <= 0)
            return Error.Validation("Muestreo.PesoPromedioMuestreadoGr", "El peso muestreado debe ser mayor a cero.");

        if (tallaPromedioMuestreadaCm <= 0)
            return Error.Validation("Muestreo.TallaPromedioMuestreadaCm", "La talla muestreada debe ser mayor a cero.");

        if (numeroPecesMuestreados <= 0)
            return Error.Validation("Muestreo.NumeroPecesMuestreados", "Debe haberse muestreado al menos un pez.");

        if (cantidadPecesVivosAlMomento < 0)
            return Error.Validation("Muestreo.CantidadPecesVivosAlMomento", "La cantidad de peces vivos no puede ser negativa.");

        if (string.IsNullOrWhiteSpace(registradoPor))
            return Error.Validation("Muestreo.RegistradoPor", "Debe indicarse quién registró el muestreo.");

        return new Muestreo
        {
            LoteId = loteId,
            Fecha = fecha,
            PesoPromedioMuestreadoGr = pesoPromedioMuestreadoGr,
            TallaPromedioMuestreadaCm = tallaPromedioMuestreadaCm,
            NumeroPecesMuestreados = numeroPecesMuestreados,
            CantidadPecesVivosAlMomento = cantidadPecesVivosAlMomento,
            RegistradoPor = registradoPor,
        };
    }

    public static Muestreo Reconstruir(
        int id, int loteId, DateOnly fecha, decimal pesoPromedioMuestreadoGr, decimal tallaPromedioMuestreadaCm,
        int numeroPecesMuestreados, int cantidadPecesVivosAlMomento, string registradoPor)
        => new()
        {
            Id = id,
            LoteId = loteId,
            Fecha = fecha,
            PesoPromedioMuestreadoGr = pesoPromedioMuestreadoGr,
            TallaPromedioMuestreadaCm = tallaPromedioMuestreadaCm,
            NumeroPecesMuestreados = numeroPecesMuestreados,
            CantidadPecesVivosAlMomento = cantidadPecesVivosAlMomento,
            RegistradoPor = registradoPor,
        };

    /// <summary>Biomasa total estimada (kg) a partir de este muestreo.</summary>
    public decimal BiomasaEstimadaKg => CantidadPecesVivosAlMomento * PesoPromedioMuestreadoGr / 1000m;

    /// <summary>
    /// Factor de Condición de Fulton: K = 100 * peso(g) / talla(cm)³.
    /// Referencia: Excel de la empresa usa K=1.123 fijo; literatura de salmónidos considera
    /// sano el rango 1.0-2.0 (docs/investigacion-parametros-produccion.md, secciones 4.1 y 7.1).
    /// </summary>
    public decimal FactorCondicionK => TallaPromedioMuestreadaCm == 0
        ? 0m
        : 100m * PesoPromedioMuestreadoGr / (decimal)Math.Pow((double)TallaPromedioMuestreadaCm, 3);
}
