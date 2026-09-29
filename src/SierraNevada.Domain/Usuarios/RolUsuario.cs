namespace SierraNevada.Domain.Usuarios;

/// <summary>Los 5 roles del sistema, acordados con el cliente (docs/arquitectura-tecnica.md sección 6).</summary>
public enum RolUsuario
{
    Administrador,
    Operario,
    Ventas,
    Logistica,
    Terceros
}
