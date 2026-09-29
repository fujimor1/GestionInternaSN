CREATE TABLE lote (
    id SERIAL PRIMARY KEY,
    codigo_lote VARCHAR(50) NOT NULL UNIQUE,
    campania_id INT NOT NULL REFERENCES campania(id),
    lote_padre_id INT NULL REFERENCES lote(id),
    unidad_produccion_id INT NULL REFERENCES unidad_produccion(id),
    bastidor_id INT NULL REFERENCES bastidor(id),
    etapa_actual VARCHAR(20) NOT NULL,
    cantidad_inicial INT NOT NULL,
    peso_promedio_inicial_gr NUMERIC(10,2) NOT NULL,
    cantidad_total_peces INT NOT NULL,
    talla_promedio_actual_cm NUMERIC(10,2) NULL,
    peso_promedio_actual_gr NUMERIC(10,2) NULL,
    fecha_ingreso_etapa DATE NOT NULL,
    fecha_fin DATE NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT chk_lote_ubicacion CHECK (unidad_produccion_id IS NOT NULL OR bastidor_id IS NOT NULL)
);

CREATE INDEX ix_lote_campania_id ON lote(campania_id);
CREATE INDEX ix_lote_lote_padre_id ON lote(lote_padre_id);
CREATE INDEX ix_lote_unidad_produccion_id ON lote(unidad_produccion_id);
