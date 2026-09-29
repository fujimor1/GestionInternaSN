CREATE TABLE registro_condiciones (
    id SERIAL PRIMARY KEY,
    lote_id INT NOT NULL REFERENCES lote(id),
    fecha DATE NOT NULL,
    temp_agua_c NUMERIC(5,2) NULL,
    ph NUMERIC(4,2) NULL,
    oxigeno_mg_l NUMERIC(5,2) NULL,
    amoniaco_mg_l NUMERIC(5,2) NULL,
    CONSTRAINT uq_registro_condiciones_lote_fecha UNIQUE (lote_id, fecha)
);
