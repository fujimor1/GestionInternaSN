CREATE TABLE unidad_produccion (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL UNIQUE,
    tipo VARCHAR(20) NOT NULL,
    sub_tipo_jaula VARCHAR(20) NULL,
    forma VARCHAR(20) NOT NULL,
    largo_m NUMERIC(10,2) NULL,
    ancho_m NUMERIC(10,2) NULL,
    diametro_m NUMERIC(10,2) NULL,
    lado_m NUMERIC(10,2) NULL,
    alto_m NUMERIC(10,2) NOT NULL,
    densidad_siembra_kg_m3 NUMERIC(10,2) NOT NULL
);
