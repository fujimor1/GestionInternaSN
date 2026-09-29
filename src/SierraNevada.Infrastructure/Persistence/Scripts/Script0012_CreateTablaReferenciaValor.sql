CREATE TABLE tabla_referencia_valor (
    id SERIAL PRIMARY KEY,
    version_id INT NOT NULL REFERENCES tabla_referencia_version(id),
    talla_min_cm NUMERIC(10,2) NOT NULL,
    talla_max_cm NUMERIC(10,2) NOT NULL,
    temperatura_min_c NUMERIC(5,2) NULL,
    temperatura_max_c NUMERIC(5,2) NULL,
    valor NUMERIC(10,4) NOT NULL
);

CREATE INDEX ix_tabla_referencia_valor_version_id ON tabla_referencia_valor(version_id);
