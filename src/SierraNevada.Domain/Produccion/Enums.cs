namespace SierraNevada.Domain.Produccion;

/// <summary>
/// Las 8 etapas reales del ciclo productivo, según el protocolo FONDEPES
/// (docs/investigacion-parametros-produccion.md, sección 3.2) — más granular
/// que el enum de 4 etapas del sistema Django anterior (OVAS/ALEVINES/JUVENILES/ENGORDE).
/// </summary>
public enum EtapaProductiva
{
    Ovas,
    AlevinajeI,
    AlevinajeII,
    AlevinajeIII,
    JuvenilesI,
    JuvenilesII,
    EngordeI,
    EngordeII
}

public enum TipoUnidadProduccion
{
    Jaula,
    Artesa
}

/// <summary>Sub-clasificación de Jaula. No aplica a Artesa.</summary>
public enum TipoJaula
{
    Juvenil,
    Engorde
}

public enum FormaUnidad
{
    Rectangular,
    Circular,
    Hexagonal,
    Decagonal
}

/// <summary>
/// Vocabulario real de la empresa (Excel) / FONDEPES, no el de Django
/// (ver docs/investigacion-parametros-produccion.md sección 5.7).
/// </summary>
public enum TipoAlimento
{
    PreInicio,
    Inicio,
    CrecimientoI,
    CrecimientoII,
    AcabadoSimple,
    AcabadoPigmento,
    Reproductores
}

public enum TipoMovimientoHistorial
{
    Creacion,
    Movimiento,
    Bajas,
    Medicion,
    CambioEtapa,
    Finalizado
}

public enum TipoTablaReferencia
{
    Racion,
    Fca,
    Mortalidad,
    Densidad
}

/// <summary>
/// De qué fuente viene una tabla de referencia. FONDEPES es el objetivo/estándar de buena
/// gestión (protocolo oficial) — Sierra Nevada es cómo opera realmente hoy la empresa (su propio
/// Excel). Los dos coexisten a propósito, no uno reemplaza al otro: la idea es poder comparar
/// "dónde estamos" contra "hacia dónde deberíamos ir" y guiar la transición gradual.
/// </summary>
public enum MarcoReferencia
{
    Fondepes,
    SierraNevada
}
