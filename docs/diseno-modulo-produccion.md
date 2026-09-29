# Diseño del nuevo módulo de producción (motor de proyección + calibración)

**Fecha:** 2026-09-21
**Propósito de este documento:** este es el diseño concreto de datos/arquitectura para el módulo de producción, construido sobre la investigación de `docs/investigacion-parametros-produccion.md` (estándares, contraste Excel/FONDEPES/Django) y los requisitos de tesis de `docs/tesis-requisitos.md`. Aquí vive el "cómo se construye", separado del "por qué" (investigación) y de lo académico (tesis).

---

## 1. Arquitectura general — 3 fases

Ya acordado en `docs/investigacion-parametros-produccion.md` sección 8.2. Resumen:

1. **Motor de proyección de campaña** — equivalente al Excel actual, con tablas semilla (FONDEPES + Excel + decisiones de la sección 6 del doc de investigación).
2. **Motor de calibración estadístico/determinístico** — con datos propios capturados, comparados contra la referencia. **Este documento diseña el modelo de datos de esta fase.**
3. **Motor adaptativo con Machine Learning** — capa futura, viable solo con suficiente historial acumulado por la fase 2. No se construye ahora; el modelo de datos de abajo está pensado para ser, a la vez, el dataset de entrenamiento futuro de esta fase.

Debe quedar **realmente desplegado** (proyecto real para cliente + tesis, ver `docs/tesis-requisitos.md`), no solo diseñado.

**Corrección (2026-09-22):** se descartó implementar nada nuevo en Django. **Todo el desarrollo, incluida la captura de datos (Campaña, Lote, Muestreo, etc.), se hace directo en el sistema nuevo (.NET + React)** — no hay paso intermedio en Django. Se verificó que `db.sqlite3` (Django actual) solo tiene datos de prueba (5 lotes, 0 ventas registradas) — no hay historial real que migrar, así que el sistema nuevo arranca con esquema limpio. Ver el plan de implementación completo en `docs/plan-implementacion-dotnet.md`.

---

## 2. Estado actual del código (`produccion/models.py`, `comercializacion/models.py`) — revisado 2026-09-21

Lo que ya existe y se reutiliza tal cual:

| Modelo actual | Para qué sirve | Se mantiene |
|---|---|---|
| `Artesa`, `Jaula` (heredan de `UnidadProduccionBiomasa`) | Calculan volumen y `capacidad_maxima_kg` con `densidad_siembra_kg_m3` | Sí, base para densidad real |
| `RegistroCondiciones` | Ya tiene `temp_agua_c`, `ph`, `oxigeno_mg_l`, `amoniaco_mg_l` por lote/día | Sí — el schema para Benson-Krause y la tabla Klontz (ración×temperatura) ya existe, solo falta que se llene en campo |
| `RegistroUnidad` | Snapshot diario por unidad (biomasa, peces, alimento, mortalidad) vía GenericForeignKey | Sí, complementa `Muestreo` |
| `HistorialMovimiento` | Log de eventos por lote (creación, movimiento, bajas, medición, finalizado) | Sí, se extiende (ver sección 4) |
| `RegistroMortalidad` | Mortalidad real por lote/fecha | Sí, tal cual |
| `RegistroVenta` / ventas (comercializacion) | Ya registra kg vendidos por lote — funciona como el "cierre" gradual del lote (FONDEPES cosecha en 4 tandas: Cabeceras/Medias/Colas) | Sí, no se duplica |

## 3. Gaps identificados y su solución

| # | Gap | Por qué importa | Solución |
|---|---|---|---|
| 1 | `Lote` sobrescribe peso/talla en cada actualización — no hay serie de tiempo | Sin historial no se puede calcular K real, FCA real, ni comparar proyectado-vs-real | Nueva tabla `Muestreo` (sección 4.3) |
| 2 | No hay alimento real en kg, solo un booleano en `RegistroDiario` | Sin esto, FCA real nunca se puede calcular, solo el "estimado simple" ya identificado como defectuoso | Nueva tabla `RegistroAlimentacionReal` (sección 4.4) |
| 3 | Ración/FCA/mortalidad/tipo de alimento están hardcodeados en Python (`Lote.racion_alimentaria_porcentaje`, `Lote.tipo_alimento`) | Si el objetivo es calibrar estas tablas con datos propios, tienen que ser datos versionados, no código fijo — si no, no hay forma de mostrar que el sistema evolucionó (clave también para la tesis) | `TablaReferenciaVersion` + `TablaReferenciaValor` (sección 4.5) |
| 4 | No hay manejo de "split" de lote (selección física por talla, o siembra repartida en varias unidades desde el inicio) | Sin esto se pierde la trazabilidad de qué grupo viene de dónde — confirmado con el cliente que sí pasa en la operación real (ver sección 5) | `Campaña` + `lote_padre` en `Lote` (secciones 4.1 y 4.2) |

## 4. Modelo de datos propuesto

### 4.1 `Campaña` (nuevo)

El nivel que faltaba por encima de `Lote`. Representa un evento de siembra — mismo origen, misma fecha, mismo lote de alevines comprado/recibido, aunque termine repartido en varias jaulas/artesas.

```
Campaña
├─ codigo (auto)
├─ fecha_siembra
├─ cantidad_alevines_sembrados
├─ peso_promedio_inicial_gr
├─ talla_promedio_inicial_cm
├─ proveedor (opcional)
└─ observaciones
```

### 4.2 `Lote` (extender el actual)

Sigue siendo "el grupo que vive en una jaula/artesa específica en un momento dado" — se agregan dos campos:

```
Lote (existente + nuevos campos)
├─ ...campos actuales (codigo_lote, etapa_actual, cantidad_total_peces, etc.)
├─ campaña        (FK → Campaña)          [NUEVO]
├─ lote_padre      (FK → Lote, self, null) [NUEVO]
└─ fecha_fin       (cuándo se cierra/divide/cosecha completo) [NUEVO]
```

Con esto, una siembra de 90,000 repartida en 3 jaulas desde el día uno = 1 `Campaña` → 3 `Lote` (uno por jaula), todos con la misma `campaña`. Y una selección a mitad de ciclo que divide un lote en dos = 2 `Lote` nuevos, cada uno con `lote_padre` apuntando al lote original (y heredando la misma `campaña`).

### 4.3 `Muestreo` (nuevo — el gap más importante)

Cada evento real de pesaje/medición (protocolo FONDEPES "Inventario", sección 8.1 del PDF fuente).

```
Muestreo
├─ lote                        (FK → Lote)
├─ fecha
├─ peso_promedio_muestreado_gr
├─ talla_promedio_muestreada_cm
├─ numero_peces_muestreados
├─ cantidad_peces_vivos_al_momento
└─ registrado_por
```

De aquí salen: K real (peso vs. talla), densidad real (biomasa derivada / volumen de la unidad), y el punto de comparación contra lo proyectado.

### 4.4 `RegistroAlimentacionReal` (nuevo, o extensión de `RegistroDiario`)

```
RegistroAlimentacionReal
├─ lote
├─ fecha
├─ cantidad_kg_entregada
└─ tipo_alimento
```

### 4.5 Transición de etapa — extensión de `HistorialMovimiento`

No requiere tabla nueva. Se agrega un tipo `CAMBIO_ETAPA` al `TIPO_MOVIMIENTO` existente, con `etapa_anterior`/`etapa_nueva` en la descripción o campos dedicados. La duración real por etapa se calcula restando fechas entre dos eventos `CAMBIO_ETAPA` consecutivos del mismo lote (o del linaje completo, siguiendo `lote_padre`).

### 4.6 `TablaReferenciaVersion` + `TablaReferenciaValor` (nuevo)

Reemplaza las propiedades hardcodeadas de `Lote` por datos versionados y auditables.

```
TablaReferenciaVersion
├─ tipo              (RACION | FCA | MORTALIDAD | DENSIDAD)
├─ fecha_vigencia_desde
├─ fuente             (ej. "FONDEPES semilla 2026-09" | "Calibrado 2027-03 con 12 lotes")
└─ activa             (bool)

TablaReferenciaValor
├─ version            (FK → TablaReferenciaVersion)
├─ talla_min / talla_max
├─ temperatura_min / temperatura_max   (opcional, para ración por Klontz 1991)
└─ valor
```

Cada recalibración crea una nueva versión con su fuente — no se pierde ni se sobrescribe la anterior. Esto es lo que permite mostrar, tanto al cliente como en la tesis, que el sistema evolucionó con datos propios de forma trazable.

---

## 5. Casos de uso que este modelo resuelve

- **Siembra repartida desde el inicio** (ej. 90,000 en varias jaulas, confirmado con el cliente que sí ocurre): 1 Campaña → N Lotes.
- **Selección a mitad de ciclo** (separar por talla, mover a otra jaula): Lote padre → Lotes hijos, misma Campaña.
- **Cálculo de densidad real**: `Muestreo.cantidad_peces_vivos_al_momento × peso_promedio_muestreado_gr / 1000` ÷ volumen de la unidad (ya calculado en `Jaula`/`Artesa`).
- **K real por lote/etapa**: derivado de pares peso/talla en `Muestreo`, no de un solo valor fijo.
- **FCA real**: `RegistroAlimentacionReal` acumulado entre dos `Muestreo` ÷ ganancia de biomasa real entre esos mismos dos puntos.
- **Duración real por etapa** ("cuánto se demoró de alevines 1 a alevines 2"): diferencia entre eventos `CAMBIO_ETAPA` consecutivos en `HistorialMovimiento`.
- **Mortalidad real por etapa**: `RegistroMortalidad` filtrado por rango de fechas de cada `CAMBIO_ETAPA`.

## 6. Siguiente paso

**Estado (2026-09-22): entidades de Domain implementadas.** Todas las entidades de este documento (Campaña, Lote con `lote_padre`, Muestreo, RegistroAlimentacionReal, HistorialMovimiento con CambioEtapa, TablaReferenciaVersion/Valor) más las reutilizadas de Django (UnidadProduccion consolidando Jaula+Artesa, Bastidor, RegistroMortalidad, RegistroCondiciones, Enfermedad) están escritas en `backend/src/SierraNevada.Domain/Produccion/`, compilando sin errores. Detalle técnico completo en `docs/arquitectura-tecnica.md`.

**Estado (2026-09-22): capa de datos + casos de uso + motor de calibración implementados y verificados con un test de integración real.** Ver `docs/arquitectura-tecnica.md` para el detalle técnico completo (incluye 2 bugs reales encontrados y corregidos durante la verificación).
