CREATE TABLE registro_alimentacion_real (
    id SERIAL PRIMARY KEY,
    lote_id INT NOT NULL REFERENCES lote(id),
    fecha DATE NOT NULL,
    cantidad_kg_entregada NUMERIC(10,2) NOT NULL,
    tipo_alimento VARCHAR(30) NOT NULL
);

CREATE INDEX ix_registro_alimentacion_real_lote_id_fecha ON registro_alimentacion_real(lote_id, fecha);
