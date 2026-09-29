CREATE TABLE registro_mortalidad (
    id SERIAL PRIMARY KEY,
    lote_id INT NOT NULL REFERENCES lote(id),
    fecha DATE NOT NULL,
    cantidad INT NOT NULL,
    registrado_por VARCHAR(255) NOT NULL
);

CREATE INDEX ix_registro_mortalidad_lote_id_fecha ON registro_mortalidad(lote_id, fecha);
