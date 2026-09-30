CREATE TABLE IF NOT EXISTS tipo_alimento (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    marca VARCHAR(50) NOT NULL,
    calibre_mm NUMERIC(4,2) NOT NULL,
    porcentaje_proteina NUMERIC(4,2) NOT NULL,
    porcentaje_grasa NUMERIC(4,2) NOT NULL,
    etapa_sugerida VARCHAR(50) NOT NULL,
    costo_unitario_promedio_kg NUMERIC(10,2) NOT NULL DEFAULT 0,
    costo_almacenamiento_anual_por_kg NUMERIC(10,2) NOT NULL DEFAULT 0.50,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS proveedor_alimento (
    id SERIAL PRIMARY KEY,
    ruc VARCHAR(20) NOT NULL UNIQUE,
    razon_social VARCHAR(150) NOT NULL,
    contacto_nombre VARCHAR(100),
    telefono VARCHAR(50),
    email VARCHAR(100),
    lead_time_dias_promedio INT NOT NULL DEFAULT 5,
    costo_orden_pedido NUMERIC(10,2) NOT NULL DEFAULT 50.00,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS lote_alimento (
    id SERIAL PRIMARY KEY,
    tipo_alimento_id INT NOT NULL REFERENCES tipo_alimento(id),
    proveedor_id INT NOT NULL REFERENCES proveedor_alimento(id),
    codigo_lote_fabrica VARCHAR(50) NOT NULL,
    fecha_fabricacion DATE,
    fecha_vencimiento DATE NOT NULL,
    peso_por_saco_kg NUMERIC(6,2) NOT NULL DEFAULT 25.00,
    cantidad_sacos_ingresados INT NOT NULL,
    cantidad_sacos_actuales INT NOT NULL,
    stock_kg_actual NUMERIC(10,2) NOT NULL,
    precio_unitario_kg NUMERIC(10,2) NOT NULL,
    fecha_recepcion TIMESTAMP NOT NULL DEFAULT NOW(),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS kardex_alimento_movimiento (
    id SERIAL PRIMARY KEY,
    tipo_alimento_id INT NOT NULL REFERENCES tipo_alimento(id),
    lote_alimento_id INT NOT NULL REFERENCES lote_alimento(id),
    fecha_movimiento TIMESTAMP NOT NULL DEFAULT NOW(),
    tipo_movimiento VARCHAR(30) NOT NULL, -- 'INGRESO_COMPRA', 'EGRESO_ALIMENTACION', 'AJUSTE_MERMA', 'DEVOLUCION'
    cantidad_kg NUMERIC(10,2) NOT NULL, -- positivo para entradas, negativo para salidas
    costo_unitario_kg NUMERIC(10,2) NOT NULL,
    costo_total NUMERIC(12,2) NOT NULL,
    saldo_stock_kg NUMERIC(10,2) NOT NULL,
    saldo_valorizado NUMERIC(12,2) NOT NULL,
    lote_produccion_id INT REFERENCES lote(id),
    observaciones TEXT,
    usuario_id INT REFERENCES usuario(id)
);

-- Datos semilla iniciales si no existen
INSERT INTO tipo_alimento (nombre, marca, calibre_mm, porcentaje_proteina, porcentaje_grasa, etapa_sugerida, costo_unitario_promedio_kg, costo_almacenamiento_anual_por_kg)
SELECT 'Inicio 0 (Polvo/Micro)', 'Nicovita', 0.5, 50.0, 12.0, 'ALEVINAJE', 8.50, 0.60
WHERE NOT EXISTS (SELECT 1 FROM tipo_alimento WHERE nombre = 'Inicio 0 (Polvo/Micro)');

INSERT INTO tipo_alimento (nombre, marca, calibre_mm, porcentaje_proteina, porcentaje_grasa, etapa_sugerida, costo_unitario_promedio_kg, costo_almacenamiento_anual_por_kg)
SELECT 'Inicio 1 (0.8mm)', 'Nicovita', 0.8, 48.0, 12.0, 'ALEVINAJE', 8.00, 0.60
WHERE NOT EXISTS (SELECT 1 FROM tipo_alimento WHERE nombre = 'Inicio 1 (0.8mm)');

INSERT INTO tipo_alimento (nombre, marca, calibre_mm, porcentaje_proteina, porcentaje_grasa, etapa_sugerida, costo_unitario_promedio_kg, costo_almacenamiento_anual_por_kg)
SELECT 'Crecimiento 2.0mm', 'Aquatec', 2.0, 45.0, 14.0, 'CRECIMIENTO', 6.80, 0.50
WHERE NOT EXISTS (SELECT 1 FROM tipo_alimento WHERE nombre = 'Crecimiento 2.0mm)');

INSERT INTO tipo_alimento (nombre, marca, calibre_mm, porcentaje_proteina, porcentaje_grasa, etapa_sugerida, costo_unitario_promedio_kg, costo_almacenamiento_anual_por_kg)
SELECT 'Crecimiento 3.0mm', 'Aquatec', 3.0, 42.0, 14.0, 'JUVENIL', 6.20, 0.50
WHERE NOT EXISTS (SELECT 1 FROM tipo_alimento WHERE nombre = 'Crecimiento 3.0mm)');

INSERT INTO tipo_alimento (nombre, marca, calibre_mm, porcentaje_proteina, porcentaje_grasa, etapa_sugerida, costo_unitario_promedio_kg, costo_almacenamiento_anual_por_kg)
SELECT 'Engorde 4.0mm', 'Aquatec', 4.0, 40.0, 15.0, 'ENGORDE', 5.90, 0.45
WHERE NOT EXISTS (SELECT 1 FROM tipo_alimento WHERE nombre = 'Engorde 4.0mm)');

INSERT INTO proveedor_alimento (ruc, razon_social, contacto_nombre, telefono, email, lead_time_dias_promedio, costo_orden_pedido)
SELECT '20100128218', 'Alicorp S.A.A. (Nicovita)', 'Ventas Acuícolas', '987654321', 'ventas@nicovita.com.pe', 4, 60.00
WHERE NOT EXISTS (SELECT 1 FROM proveedor_alimento WHERE ruc = '20100128218');

INSERT INTO proveedor_alimento (ruc, razon_social, contacto_nombre, telefono, email, lead_time_dias_promedio, costo_orden_pedido)
SELECT '20501489721', 'Aquatec Feed Perú S.A.C.', 'Distribuidor Central', '976543210', 'contacto@aquatecfeed.pe', 5, 50.00
WHERE NOT EXISTS (SELECT 1 FROM proveedor_alimento WHERE ruc = '20501489721');
