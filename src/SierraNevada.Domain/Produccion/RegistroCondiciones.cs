using ErrorOr;

namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Condiciones de agua por lote/día (temperatura, pH, oxígeno, amoniaco). El campo de
/// temperatura es el que habilita la tabla de ración por talla×temperatura (Klontz 1991,
/// docs/investigacion-parametros-produccion.md sección 7.6) — hoy nadie la usa, pero el dato
/// se puede capturar desde ya. La corrección de oxígeno por altitud (fórmula Benson-Krause,
/// sección 7.5) se calcula en Application, no aquí — depende de la altitud de la granja,
/// que es configuración externa, no un dato intrínseco del registro.
/// </summary>
public sealed class RegistroCondiciones
{
    public int Id { get; private set; }
    public int LoteId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public decimal? TempAguaC { get; private set; }
    public decimal? Ph { get; private set; }
    public decimal? OxigenoMgL { get; private set; }
    public decimal? AmoniacoMgL { get; private set; }

    private RegistroCondiciones() { }

    public static ErrorOr<RegistroCondiciones> Crear(
        int loteId, DateOnly fecha, decimal? tempAguaC, decimal? ph, decimal? oxigenoMgL, decimal? amoniacoMgL)
    {
        if (tempAguaC is null && ph is null && oxigenoMgL is null && amoniacoMgL is null)
            return Error.Validation("RegistroCondiciones.Datos", "Debe registrarse al menos un parámetro.");

        return new RegistroCondiciones
        {
            LoteId = loteId,
            Fecha = fecha,
            TempAguaC = tempAguaC,
            Ph = ph,
            OxigenoMgL = oxigenoMgL,
            AmoniacoMgL = amoniacoMgL,
        };
    }

    public static RegistroCondiciones Reconstruir(
        int id, int loteId, DateOnly fecha, decimal? tempAguaC, decimal? ph, decimal? oxigenoMgL, decimal? amoniacoMgL)
        => new() { Id = id, LoteId = loteId, Fecha = fecha, TempAguaC = tempAguaC, Ph = ph, OxigenoMgL = oxigenoMgL, AmoniacoMgL = amoniacoMgL };
}
