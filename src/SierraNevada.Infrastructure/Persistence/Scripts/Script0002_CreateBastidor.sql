CREATE TABLE bastidor (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL UNIQUE,
    capacidad_maxima_unidades INT NOT NULL,
    esta_disponible BOOLEAN NOT NULL DEFAULT TRUE
);
