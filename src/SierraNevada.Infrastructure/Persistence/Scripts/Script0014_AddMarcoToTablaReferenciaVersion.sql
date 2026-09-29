-- Antes solo había una versión "activa" por tipo (Ración/FCA/Mortalidad/Densidad), pensada para
-- ir reemplazando la anterior a medida que se recalibra. Ahora conviven dos marcos en paralelo
-- por tipo: FONDEPES (objetivo/buena gestión, protocolo oficial) y SierraNevada (cómo opera
-- realmente hoy la empresa, según su propio Excel) — para poder comparar uno contra el otro,
-- no que uno reemplace al otro.
ALTER TABLE tabla_referencia_version ADD COLUMN marco VARCHAR(20) NOT NULL DEFAULT 'Fondepes';
ALTER TABLE tabla_referencia_version ALTER COLUMN marco DROP DEFAULT;

DROP INDEX uq_tabla_referencia_version_activa;

CREATE UNIQUE INDEX uq_tabla_referencia_version_activa
    ON tabla_referencia_version(tipo, marco)
    WHERE activa = TRUE;
