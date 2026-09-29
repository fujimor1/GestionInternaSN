-- Nombre de tabla ASCII-safe ("campania", sin ñ) por portabilidad de herramientas/locale del VPS;
-- la clase de dominio se sigue llamando Campaña (idioma ubicuo), el repositorio mapea entre ambos.
CREATE TABLE campania (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL UNIQUE,
    fecha_siembra DATE NOT NULL,
    cantidad_alevines_sembrados INT NOT NULL,
    peso_promedio_inicial_gr NUMERIC(10,2) NOT NULL,
    talla_promedio_inicial_cm NUMERIC(10,2) NOT NULL,
    proveedor VARCHAR(255) NULL,
    observaciones TEXT NULL
);
