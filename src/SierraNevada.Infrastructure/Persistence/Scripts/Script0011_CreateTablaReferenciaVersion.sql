CREATE TABLE tabla_referencia_version (
    id SERIAL PRIMARY KEY,
    tipo VARCHAR(20) NOT NULL,
    fecha_vigencia_desde DATE NOT NULL,
    fuente VARCHAR(255) NOT NULL,
    activa BOOLEAN NOT NULL DEFAULT FALSE
);

-- Solo una versión activa por tipo a la vez (auditabilidad de la calibración).
CREATE UNIQUE INDEX uq_tabla_referencia_version_activa
    ON tabla_referencia_version(tipo)
    WHERE activa = TRUE;
