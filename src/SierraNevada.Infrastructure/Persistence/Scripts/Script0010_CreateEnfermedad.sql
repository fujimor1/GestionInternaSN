CREATE TABLE enfermedad (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL UNIQUE,
    descripcion TEXT NOT NULL,
    tratamiento TEXT NOT NULL,
    prevencion TEXT NOT NULL
);
