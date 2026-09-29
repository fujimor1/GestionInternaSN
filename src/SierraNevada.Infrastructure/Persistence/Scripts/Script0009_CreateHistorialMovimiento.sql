CREATE TABLE historial_movimiento (
    id SERIAL PRIMARY KEY,
    lote_id INT NOT NULL REFERENCES lote(id),
    fecha TIMESTAMP NOT NULL,
    tipo_movimiento VARCHAR(20) NOT NULL,
    descripcion VARCHAR(255) NOT NULL,
    cantidad_afectada INT NULL,
    etapa_anterior VARCHAR(20) NULL,
    etapa_nueva VARCHAR(20) NULL
);

CREATE INDEX ix_historial_movimiento_lote_id_fecha ON historial_movimiento(lote_id, fecha DESC);
