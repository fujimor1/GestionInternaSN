CREATE TABLE muestreo (
    id SERIAL PRIMARY KEY,
    lote_id INT NOT NULL REFERENCES lote(id),
    fecha DATE NOT NULL,
    peso_promedio_muestreado_gr NUMERIC(10,2) NOT NULL,
    talla_promedio_muestreada_cm NUMERIC(10,2) NOT NULL,
    numero_peces_muestreados INT NOT NULL,
    cantidad_peces_vivos_al_momento INT NOT NULL,
    registrado_por VARCHAR(255) NOT NULL
);

CREATE INDEX ix_muestreo_lote_id_fecha ON muestreo(lote_id, fecha);
