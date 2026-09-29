# Investigación y contraste de parámetros técnicos de producción (Truchicultura)

**Fecha:** 2026-09-18 / 2026-09-19
**Contexto:** Antes de migrar el sistema a C# .NET + React, se contrastó la lógica de negocio actual del módulo `produccion` (Django, `produccion/models.py` y `produccion/views.py`) contra: (1) literatura técnica/oficial de truchicultura andina, y (2) el archivo real `Sierra Nevada.xlsx` con el que la empresa maneja la alimentación de sus campañas. El objetivo es tener registrado **de dónde salió cada dato** y **qué se contrastó contra qué**, para no perder el hilo cuando se retome este análisis o se diseñe el nuevo sistema.

La granja opera a **~4,500 msnm**. La referencia oficial más cercana encontrada (FONDEPES, Laguna Lagunillas, Puno) está a 4,174 msnm — el contexto altoandino es comparable.

---

## 1. Fuentes consultadas

| # | Fuente | Tipo | Dato clave que aportó |
|---|---|---|---|
| 1 | [Protocolo de Engorde de Truchas Arco Iris — Cultivo en Jaulas Flotantes (FONDEPES, Puno, 2009)](https://rnia.produce.gob.pe/wp-content/uploads/2019/09/Protocolo-de-engorde-de-trucha-arco-iris.-Cultivo-de-jaulas-flotantes..pdf) — **PDF completo guardado en `docs/fuentes/fondepes-protocolo-engorde-trucha-2009.pdf`** (leído íntegro el 2026-09-21, ya no depende solo del link) | Protocolo oficial del Estado peruano (FONDEPES), zona Laguna Lagunillas, 4,174 msnm | Tabla de tasas de alimentación por etapa/talla, **duración exacta de cada etapa**, densidad final de cultivo (13–15 kg/m³), parámetros óptimos de agua (O₂, pH, temperatura, amoniaco), tipos de alimento y % proteína por etapa, tabla de ración por talla×temperatura (Klontz 1991), mortalidad esperada del ciclo en condiciones óptimas (3–5%), ejemplo mensual completo de programa de producción |
| 2 | [Determinación del FCA y crecimiento de trucha arco iris bajo diferentes densidades — Llaullini, La Paz (Revista Apthapi, 2020)](http://www.revistasbolivianas.ciencia.bo/pdf/apt/v6n1/v6n1_a08.pdf) | Estudio académico, Altiplano boliviano, 3,634 msnm, estanques (no jaulas) | Fórmulas estándar de crecimiento (GP, GL), Factor de Condición de Fulton, fórmula de FCA, densidades de siembra en estanques de flujo continuo (0.61–2.05 kg/m³), FCA real medido (0.42 en baja densidad) |
| 3 | [Diagnóstico situacional de la crianza de truchas arco iris (UPCH)](https://repositorio.upch.edu.pe/bitstream/handle/20.500.12866/3862/Diagnostico_MontesinosLopez_Jeansen.pdf) | Tesis/diagnóstico, Perú | Contexto general de densidades de cultivo en Puno (6–12 kg/m³ operando sin problemas) |
| 4 | Búsquedas web generales (WebSearch) sobre: densidad de siembra en jaulas flotantes, tablas de alimentación por peso corporal, clasificación de alimento balanceado por etapa, efecto de la altitud en la saturación de oxígeno disuelto | Múltiples resultados agregados (ver conversación original para links individuales) | Confirmación cruzada de rangos de densidad (26–30 kg/m³ en otras condiciones no altoandinas), rangos generales de ración (1–3% en engorde, 5–12% en alevinaje), relación presión atmosférica ↔ solubilidad de oxígeno |
| 5 | `Sierra Nevada.xlsx` (archivo real de la empresa, proporcionado por el usuario) | Dato primario de la propia empresa | Fórmulas reales de Excel usadas para proyectar ración, FCA objetivo, mortalidad y tipo de dieta por talla, en 5 hojas de campaña (`canpaña 4`, `canpaña 1 _90000`, `jaula roja 69000`, `campaña 2`, `campaña 1`) |
| 6 | Deslauriers, D., Chipps, S.R., Breck, J.E., Rice, J.A. & Madenjian, C.P. (2017). "Fish Bioenergetics 4.0: An R-Based Modeling Application." *Fisheries*, 42(11), 586–596 — **pendiente de descargar el PDF/DOI exacto y guardarlo en `docs/fuentes/`, citado aquí de memoria por el asistente, verificar antes de usarlo en la tesis** | Paper académico, modelo bioenergético de referencia | Set de parámetros fisiológicos publicado para *Oncorhynchus mykiss* (trucha arcoíris) — consumo, respiración, crecimiento en función de temperatura y ración; base propuesta para la capa mecanicista de la fase 3 (sección 8.2.1) |
| 7 | Gelman, A., Carlin, J.B., Stern, H.S., Dunson, D.B., Vehtari, A. & Rubin, D.B. (2013). *Bayesian Data Analysis* (3ra ed.), capítulos 2–3. CRC Press — **libro de referencia estándar, citado de memoria por el asistente, verificar edición/ISBN exactos antes de usarlo en la tesis** | Libro de texto académico, estadística bayesiana | Actualización conjugada Normal-Normal y Beta-Binomial — base propuesta para la capa de calibración estadística de la fase 3 (sección 8.2.1) |

**Nota:** los archivos PDF no se pudieron leer directamente con WebFetch (errores de certificado / parseo de binario); se descargaron y se extrajeron con `pdftotext` (paquete `poppler-utils`, ya disponible en el entorno vía `mingw64`).

---

## 2. Qué hace el sistema Django actual (línea base)

Archivo: `produccion/models.py`

- `UnidadProduccionBiomasa` (abstracta, heredada por `Artesa` y `Jaula`): calcula `capacidad_maxima_kg = volumen × densidad_siembra_kg_m3`, con `densidad_siembra_kg_m3` por defecto **10.0 kg/m³**, fijo, igual para ambos tipos de unidad y sin variar por etapa.
- `Lote.racion_alimentaria_porcentaje`: tabla por **peso** (gr):
  `≤20g→2.8% · ≤50g→2.5% · ≤100g→2.2% · ≤150g→1.9% · ≤250g→1.5% · >250g→1.2%`
- `Lote.tipo_alimento`: por **talla** (cm):
  `≤8cm→"Alevines 1" · 8–10cm→"Alevines 2" · 10–15cm→"Crecimiento 1" · 15–20cm→"Crecimiento 2" · >20cm→"Engorde"`
- `Lote.conversion_alimenticia` (FCA): se calcula **hacia atrás** — `alimento_total_consumido_kg / biomasa_ganada_kg`, donde el alimento consumido se estima como `alimento_diario_de_hoy × días_en_etapa` (asume ración constante desde el día 1; el propio código lo señala como "estimado simple").
- `RegistroMortalidad`: mortalidad **observada/registrada manualmente** por el usuario, no proyectada.
- `talla_min_cm`/`talla_max_cm` y `peso_promedio_pez_gr` son campos **independientes** en `Lote`, sin relación matemática entre ellos.
- El módulo de IA (`produccion/ia/`) está roto y no se usa (ver hallazgo aparte, fuera del alcance de este documento — el usuario ya decidió eliminarlo en la migración).

---

## 3. Lo que dice la literatura (FONDEPES / Bolivia)

### 3.1 Densidad de cultivo
- FONDEPES: densidad final de cosecha en jaulas flotantes (Engorde II) = **13–15 kg/m³**.
- Región Puno en general: opera sin problemas entre **6–12 kg/m³**; Lagunillas–FONDEPES llegó a 13 kg/m³ sin incidentes.
- Bolivia (Llaullini, estanques de flujo continuo, no jaulas): **0.61–2.05 kg/m³** — mucho menor, porque el sistema de recambio de agua es distinto (12 renovaciones/día).
- Otras fuentes (condiciones no altoandinas, no citadas como comparables directas): 26–30 kg/m³.

### 3.2 Tasa de alimentación y duración por etapa/talla — FONDEPES Tabla 5 + Diagrama de flujo (sección VIII)
| Etapa | Talla | Tasa | **Duración** |
|---|---|---|---|
| Alevinaje I | 3.5–5cm | 5–12% | **1 a 1.5 meses** |
| Alevinaje II | 5–8cm | 3–4% | **1 mes** |
| Alevinaje III | 8–12cm | 2–3% | **1 mes** |
| Juveniles I | 12–14cm | 2–3% | **1 mes** |
| Juveniles II | 14–17cm | 1.8–2.1% | **1 mes** |
| Engorde I | 17–20cm | 1.1–1.6% | **2 meses** |
| Engorde II | 20cm+ (comercial 26cm+) | 1.1–1.6% | **2 meses o más, según talla objetivo** |

**Total ciclo: ~9 a 9.5 meses** de siembra a cosecha — el protocolo lo confirma directamente: *"La cosecha deberá estar programada entre los meses 9 y 12"* (sección 8.2.9). Esta columna de duración es una cita directa del protocolo (antes solo se había inferido de un cronograma ambiguo, ver sección 5.3).

### 3.3 Tipos de alimento y % proteína — FONDEPES Tabla 3/4
Pre-inicio, Inicio 1, Inicio 2, Crecimiento 1, Crecimiento 2, Engorde, Acabado — con % proteína 45/45/45/42/42/40/40 y tamaño de partícula desde 1.5×0.8mm hasta 5×6mm, clasificados por **peso** (no talla): <1g, 1–5g, 5–25g, 25–66.6g, 66.6g–comercial.

### 3.4 Parámetros óptimos de agua — FONDEPES Tabla 1 (y valores reales en Lagunillas, 4,174msnm)
| Parámetro | Óptimo (tabla general) | Real en Lagunillas |
|---|---|---|
| Oxígeno disuelto | >90mmHg pO₂ (~7mg/L a 60% saturación) | >85mmHg pO₂ |
| pH | 7–8 | Neutro (~7) |
| Temperatura | 11–16°C | 10–15°C |
| Amoniaco (N) | <1.0mg/L total; <0.05mg/L intermitente / <0.03mg/L constante (no ionizado) | — |

### 3.5 Mortalidad
FONDEPES lo dice dos veces, sin ambigüedad (ya no es una inferencia): *"La mortalidad estimada para todo el proceso productivo deberá estar entre el 3 – 5%"* (sección 8.2.8) y, en el ejemplo de programa de producción, *"La mortalidad estimada **en condiciones óptimas de cultivo** es de 3 – 5% durante todo el proceso productivo"*. Es decir: **3–5% es del ciclo completo (9 meses) Y es la cifra que FONDEPES asocia a buen manejo/condiciones óptimas**, no un promedio genérico a superar — ver corrección en sección 7.3.

### 3.6 FCA
Fórmula estándar (Bolivia, ec. 4, cita a FONDEPES 2014): `FCA = Alimento suministrado / Ganancia de peso`. FCA real medido en el estudio boliviano: **0.42** (densidad muy baja, 0.61 kg/m³) — mejor que el promedio típico comercial (1.0–1.5 citado en literatura general).

### 3.7 Altitud y oxígeno
A mayor altitud, baja la presión atmosférica y con ella la solubilidad/saturación de oxígeno en el agua. El mismo valor en mg/L representa un % de saturación **menor** a 4,500msnm que a 4,174msnm (Lagunillas) o a nivel del mar. FONDEPES mide oxígeno en mmHg de pO₂ / % de saturación, no solo mg/L crudos, precisamente por esto.

---

## 4. Lo que hace realmente la empresa (Sierra Nevada.xlsx)

El Excel **no es un registro histórico** — es un **motor de proyección/simulación de campaña**, día por día. Las 5 hojas (`canpaña 4`, `canpaña 1 _90000`, `jaula roja 69000`, `campaña 2`, `campaña 1`) usan **exactamente las mismas fórmulas maestras** (confirmado comparando las fórmulas en columnas D–M de cada hoja) — es un modelo estandarizado de la empresa, no algo improvisado por campaña.

### 4.1 Cadena de cálculo diario (columnas del Excel)
```
D  PESO UNITARIO (g)     = 1000 / C            (C = peces por kg)
E  TALLA UNITARIA (cm)   = (D / 0.01123) ^ (1/3)      ← Factor de Condición de Fulton, K=1.123 FIJO
F  NUMERO TOTAL DE PECES = F(ayer) - L(ayer)          (peces vivos = ayer - muertos)
G  % RACION ALIMENTICIA  = lookup por talla (E)
H  CONSUMO ALIMENTO (kg) = BIOMASA(ayer) × G / 100
I  CONV. ALIM. (FCA obj.)= lookup por talla (E)       ← FCA usado como META, no resultado
J  GANANCIA EN PESO (kg) = H / I
B  BIOMASA (kg)          = B(ayer) + J(ayer)
K  % MORTALIDAD          = lookup por talla (E), tasa MENSUAL, dividida entre 32 (días/mes asumidos)
L  PECES MUERTOS         = F(ayer) × K / 100
M  TIPO DE DIETA         = lookup por talla (E)
```

### 4.2 Tabla real de ración por talla (reconstruida desde las fórmulas de columna G)
| Talla (cm) | Ración | Dieta (columna M) |
|---|---|---|
| <4 | 6% | pre inicio |
| 4–5.01 | 5% | inicio |
| 5.61–7.07 | 4% | inicio |
| 7.07–9.45 | 3.5% | crecimiento I |
| 9.45–11.82 | 2.8% | crecimiento I |
| 11.82–14.18 | 2.5% | crecimiento II |
| 14.18–16.54 | *(hueco — la fórmula no cubre este tramo, bug propio del Excel)* | — |
| 16.54–18.9 | 2.2% | crecimiento II |
| 18.9–21.26 | 1.9% | acabado simple |
| 21.26–23.63 | 1.5% | acabado simple |
| 23.63–25.97 | 1.4% | acabado pigmento |
| 25.97–30 | 1.2% | acabado pigmento |
| >30 | 1% | reproductores |

### 4.3 FCA objetivo asumido por talla (columna I)
`0.2–7.07cm → 1.0` · `7.07–11.82cm → 1.1` · `11.82–18.9cm → 1.13` · `>18.9cm → 1.14`
Nota en el propio Excel: *"CA Promedio (kg Alim/kg trucha) Máximo 1.15"*.

### 4.4 Mortalidad mensual asumida por talla (columna K, antes de dividir entre 32)
`2–4cm → 5%` · `4–5.61cm → 4.5%` · `5.61–7.07cm → 4%` · `7.07–9.45cm → 3.5%` · `9.45–11.82cm → 3%` · `11.82–14.18cm → 2.5%` · `>14.18cm → 2%`
(Se observó una variante con constantes distintas — 8, 7.5, 7, 6, 5, 4, 2 — en una fila intermedia de `campaña 1`, posiblemente una versión anterior de la tabla que quedó sin actualizar en esa celda puntual; no se confirmó si está activa en producción del Excel.)

### 4.5 Producción/consumo total proyectado (panel lateral de `campaña 1`)
- Volumen de producción por lote: 50 toneladas (50,000 kg)
- Cantidad de alimento total proyectado: 57,670.58 kg

---

## 5. Contraste: Excel (real) vs. Investigación (FONDEPES / Bolivia)

### 5.1 Ración alimenticia por talla
| Talla (cm) | Excel (real) | FONDEPES (oficial) | Veredicto |
|---|---|---|---|
| <5 | 5–6% | Alevinaje I: 5–12% | ✅ Excel usa el extremo bajo del rango oficial — conservador, dentro de norma |
| 5.6–9.4 | 3.5–4% | Alevinaje II: 3–4% | ✅ Coincide casi exacto |
| 9.4–14.2 | 2.5–2.8% | Alevinaje III / Juv. I: 2–3% | ✅ Coincide, dentro de rango |
| 14.2–16.5 | *(hueco)* | Juveniles II: 1.8–2.1% | ⚠️ Excel no define este tramo |
| 16.5–18.9 | 2.2% | Juveniles II: máx 2.1% | Ligeramente por encima (0.1pp) |
| 18.9–21.3 | **1.9%** | Engorde I: 1.1–1.6% | ❌ **Discrepancia real**: la empresa alimenta más que el techo oficial en la transición a engorde |
| 21.3–30 | 1.2–1.5% | Engorde II: 1.1–1.6% | ✅ Vuelve a calzar bien |

**Conclusión:** la tabla real de la empresa sigue la misma lógica y magnitud del protocolo oficial en casi todo el ciclo (no es una tabla inventada). La única discrepancia significativa y verificable es la sobrealimentación puntual (1.9% vs 1.6%) alrededor de 19–21cm.

### 5.2 FCA
Los valores objetivo del Excel (1.0–1.14, tope 1.15) son **realistas y conservadores** frente a la literatura (FCA real medido: 0.42 en baja densidad; rango comercial típico citado: 1.0–1.5). No hay señal de mala calibración aquí — el problema identificado es que **Django no usa esta meta para nada**, calcula el FCA al revés (después del hecho, con un método que el propio código admite como aproximado).

### 5.3 Mortalidad
**Resuelto (2026-09-21, ver sección 7.3):** FONDEPES confirma con cita directa que el 3–5% es del **ciclo completo** (~9 meses, tabla de duración en sección 3.2) y que corresponde a **condiciones óptimas de cultivo**, no a un promedio cualquiera. Sobre 9 meses, una tasa mensual compuesta de 2–5% (como usa el Excel en sus etapas tempranas) da un acumulado muy superior a 3–5% — esto confirma que **las tasas mensuales tempranas del Excel (5–8%) son altas frente al estándar de buen manejo de FONDEPES**, no solo frente a un promedio genérico. Sigue pendiente con el cliente confirmar si su histórico interno coincide con esta lectura.

### 5.4 Factor de Condición (relación Talla–Peso)
- Excel: K=1.123 fijo para todo el ciclo.
- Bolivia (Llaullini): con sus datos reportados (140g, 21.5cm) el K real medido es ≈**1.41**.
- **Hallazgo accionable:** el K del Excel es notablemente más bajo (peces "más flacos" en el modelo) que el K observado en un estudio comparable de altura. Dos lecturas posibles: (a) la conformación real de las truchas de Sierra Nevada es distinta, o (b) el K=1.123 quedó fijado hace tiempo y ya no refleja la condición real de los lotes actuales. **Se recomienda tomar una muestra real de peso/talla y recalcular K** antes de portar esta constante al nuevo sistema — todo el modelo de ración/FCA/mortalidad depende de la talla derivada de esta constante, así que un K desactualizado desplaza todos los lookups al tramo equivocado.

### 5.5 Densidad de siembra
El Excel **no modela densidad volumétrica** (no tiene columna de m³ ni kg/m³) — solo maneja biomasa total y conteo de peces. El control de densidad (13–15 kg/m³ según FONDEPES) vive completamente fuera de esta herramienta. Confirma que en la operación real son dos preocupaciones separadas: proyección de alimentación/crecimiento (Excel) vs. control de capacidad física de la jaula (no automatizado en ningún lado actualmente).

### 5.6 Calidad de agua / altitud
El Excel no registra temperatura, pH, oxígeno ni amoniaco — no hay con qué contrastar aquí. El hallazgo sobre ajuste de oxígeno por altitud (4,500msnm) sigue siendo válido solo contra la literatura (FONDEPES), y no está cubierto ni por el Excel ni por el sistema Django actual.

### 5.7 Nomenclatura de dietas/etapas
| Django | Excel real / FONDEPES |
|---|---|
| Alevines 1, Alevines 2 | pre inicio, inicio |
| Crecimiento 1, Crecimiento 2 | crecimiento I, crecimiento II |
| Engorde | acabado simple, acabado pigmento, reproductores |

Django usa una nomenclatura propia que no coincide con la que la empresa ya usa en su Excel (que a su vez coincide con FONDEPES). **Se recomienda adoptar el vocabulario real de la empresa en el sistema nuevo.**

---

## 6. Preguntas abiertas para validar con el cliente antes de diseñar el nuevo sistema

1. ~~¿El K=1.123 (factor de condición) sigue siendo válido, o hay que recalcularlo con datos actuales de sus lotes?~~ **Respondido por el cliente (2026-09-21):** es un valor de referencia que la propia empresa ha ido recopilando empíricamente en el tiempo (no viene de una fuente externa). Como cae dentro del rango "sano" de la literatura (1.0–2.0, sección 7.1), **se mantiene como valor semilla por ahora**. **Decisión de diseño:** a futuro debe recalcularse (a) por cada lote a su cosecha, y (b) idealmente por etapa — hoy es un único valor fijo para todo el ciclo, sin cálculo diferenciado por talla/etapa, así que su precisión real en cada tramo es desconocida. Esto implica que el motor de captura debe tomar muestras de peso/talla en varios puntos del ciclo (no solo al final del lote) para poder derivar un K por etapa, no uno solo global.
2. ~~¿El 3–5% de mortalidad que manejan como referencia es del ciclo completo o solo de la etapa de engorde?~~ **Resuelto en sección 7.3**: el manual oficial RNIA/PRODUCE 2022 confirma que es del ciclo completo. Queda pendiente con el cliente solo confirmar si su histórico interno coincide con esa lectura.
3. ~~¿La sobrealimentación detectada en la transición juvenil→engorde (1.9% vs 1.6% oficial) es intencional (por alguna razón operativa local) o es un ajuste que valdría la pena revisar?~~ **Respondido por el cliente (2026-09-21):** sí, es una práctica intencional de la empresa. Se deja marcada como **pendiente de decisión** (mantenerla o ajustarla en el nuevo sistema), no como pregunta de investigación.
4. ~~¿Cómo controlan hoy la densidad máxima por jaula/artesa, si no está en este Excel? ¿Hay otra planilla o es manejo empírico?~~ **Respondido por el cliente (2026-09-21):** no controlan la densidad máxima de forma activa hoy. El rango 13–15 kg/m³ citado en este documento (sección 3.1) es solo la referencia investigada de FONDEPES — no es una cifra que la empresa mida o gestione actualmente. **Es una laguna real de control en la operación actual**, a considerar como oportunidad de mejora en el nuevo sistema (no solo migración, sino una capacidad nueva).
5. ~~¿Miden oxígeno disuelto en mg/L o en % de saturación / mmHg?~~ **Respondido por el cliente (2026-09-21):** antes medían oxígeno disuelto, pero **actualmente ya no lo hacen**. Sin medición activa, la corrección por altitud (fórmula Benson-Krause, sección 7.5) queda como algo a implementar **junto con la reinstauración de esa medición**, no algo aplicable a datos actuales — no hay con qué calcular todavía.

---

## 7. Estándares de calidad y salud del pez (investigación complementaria — altura extrema)

**Fecha:** 2026-09-21
**Contexto:** el objetivo del nuevo sistema no es solo replicar el Excel o adaptarse a lo que ya hace la empresa — es definir **estándares de calidad/salud reales** hacia los que el sistema debe calibrar (qué significa que una trucha esté "sana y fuerte", no solo viva), y usar eso para ir reduciendo la mortalidad hacia un piso conocido, no aceptar el promedio histórico como bueno. Esta sección amplía la investigación de las secciones 1-6 con esa pregunta específica.

### 7.1 Indicadores de salud más allá del K de Fulton
En salmónidos, K < 1.0 se considera indicador de pez en mal estado nutricional/físico; K entre 1.0–2.0 se considera aceptable a bueno (estudios de steelhead en jaulas reportan K=1.29–1.64). **El K=1.123 fijo del Excel de Sierra Nevada está en el límite bajo de ese rango "sano"** — refuerza el hallazgo de la sección 5.4: amerita recalcularse con muestras reales. Otros indicadores usados en la industria (índice hepatosomático, hematocrito, cortisol) son conceptualmente útiles pero no medibles en operación diaria de campo — el sistema puede aproximarse a "salud" con K + FCA + mortalidad por período como proxies indirectos y sí medibles. *(Fuente genérica de fisiología de salmónidos, no específica de altura andina — aplicar con cautela).*

### 7.2 Altitudes comparables o superiores a 4,500msnm
No existe literatura pública con datos técnicos de **jaulas flotantes en laguna a ≥4,500msnm**. La referencia de mayor altitud encontrada con datos concretos es un estudio de trucha en **sistema RAS a 3,000msnm** (Centro Copaquilla Pukara, norte de Chile) — sistema distinto (recirculación con oxigenación forzada, no jaula abierta), pero es el dato de altura más alta disponible: supervivencia 98.33%/12 meses, FCR 1.33, oxígeno 4.9–7.0 mg/L, compensado con generador de O₂ y aireación forzada. Hay proyectos peruanos reales >3,500msnm (Tacna, Apurímac) pero sin datos técnicos publicados. **Conclusión: Sierra Nevada opera en el borde superior de lo documentado mundialmente — sus propios datos acumulados van a tener más valor que cualquier tabla externa.**

### 7.3 Benchmarks de mortalidad con buenas prácticas
El manual **RNIA/PRODUCE 2022** ("Manual para una acuicultura sostenible — Cultivo de Trucha") confirma: *"La mortalidad estimada para todo el proceso productivo se encuentra en el rango del 3% al 5% en condiciones normales de crianza"* — del **ciclo completo**, no solo de engorde. Esto resuelve la pregunta abierta #2.

**Corrección (2026-09-21, tras leer el PDF completo de FONDEPES 2009 — ver sección 3.5):** el protocolo original de FONDEPES es más específico que "condiciones normales" — dice textualmente *"en condiciones óptimas de cultivo"*. Es decir, **3–5% ya es la cifra que la industria asocia a buen manejo**, no un promedio a superar fácilmente. El caso chileno (RAS, 3,000msnm) logró 1.67% con control ambiental fuerte (oxigenación forzada, biofiltro) — sigue siendo un techo aspiracional válido, pero no comparable directo (sistema distinto). **Meta recomendada, ajustada:** tratar 3-5% como el estándar de buena práctica ya reconocido por la industria (no un piso fácil de superar) y usar el caso RAS solo como referencia de cuánto puede bajar con inversión en control ambiental — la meta realista de corto plazo para Sierra Nevada es *alcanzar* 3-5% de forma consistente, no asumir que ya lo superan.

### 7.4 FCA y su relación con salud
FCR global histórico en truchicultura: rango 0.7–2.0, promedio ~1.25. El FCA objetivo del Excel (1.0–1.14) está en el extremo eficiente de ese rango. El caso chileno en altura obtuvo FCR 1.33 y lo consideró "satisfactorio" dado el estrés ambiental de la altitud — sugiere que a mayor altitud puede haber tolerancia esperada de un FCA algo peor por estrés fisiológico, sin que eso signifique mal manejo. Un FCA fuera de rango (muy alto) sí es señal de alerta temprana (sobrealimentación, estrés, alimento de mala calidad) — es dinámico, no una constante fija, lo que refuerza el hallazgo de la sección 2 de que Django lo calcula "al revés" y no lo usa como alerta.

### 7.5 Corrección de oxígeno disuelto por altitud
Existe una fórmula técnica estándar y reconocida — **Benson & Krause (1984)**, adoptada por USGS — para calcular saturación de oxígeno en función de temperatura, salinidad y presión atmosférica/altitud, con precisión ±0.01 mg/L. No hay una tabla pre-hecha para 4,500msnm exactos, pero esta fórmula permite calcularlo con rigor conociendo temperatura del agua y presión barométrica local. Es directamente accionable para el nuevo sistema: reemplazar el registro de oxígeno en mg/L crudos (que hoy no existe en ningún lado) por % de saturación real vía esta fórmula, igual que FONDEPES usa mmHg de pO₂ en Lagunillas.

### 7.6 Calidad de agua en altura extrema — y una brecha real encontrada
El manual RNIA/PRODUCE 2022 da rangos muy similares a los de FONDEPES ya documentados (temp 10-17°C, O₂ >5.5-7.0mg/L según etapa, pH 6.5-9.0, amoniaco <0.02mg/L) — ninguna fuente diferencia estos rangos por altitud extrema, la brecha de conocimiento publicado sigue abierta por encima de ~4,000msnm.

**Hallazgo más importante de esta sección:** la tabla estándar de la industria (Cuadro 17, fuente Klontz 1991) varía la ración (% biomasa/día) **por talla Y por temperatura del agua simultáneamente** — a menor temperatura, la ración óptima cae para la misma talla. **Ni el Excel ni Django ajustan la ración por temperatura del agua**, solo por talla/peso. A 4,500msnm la temperatura del agua fluctúa de forma importante (el caso chileno reporta 7.4°C–18°C estacional) — según este estándar, eso debería cambiar cuánto alimentar, no solo la talla del pez. **Es una brecha real y accionable para el diseño del nuevo sistema**, no solo una discrepancia entre fuentes.

### 7.7 Resumen de aplicabilidad

| Hallazgo | Específico de altura andina | Genérico — adaptar con cautela |
|---|---|---|
| K "sano" 1.0–2.0 | No | Sí |
| Altitud 3,000msnm Chile (RAS) | Sí (más cercano en altitud, distinto en sistema) | — |
| Mortalidad 3–5% = ciclo completo | Sí (RNIA/PRODUCE Perú) | — |
| FCA 1.0–1.14 vs FCR 1.33 en altura | Sí (comparación directa) | — |
| Fórmula Benson-Krause (O₂) | No, pero es la fórmula técnica correcta para cualquier altitud | — |
| Ración por talla×temperatura (Klontz 1991) | No — pero el *principio* sí aplica y es una brecha real | Sí |
| Calidad de agua (Cuadro 4 RNIA) | Parcial — Perú, sin diferenciar por altitud extrema | — |

**Nota:** no se encontró información adicional para las preguntas abiertas #4 (control de densidad sin modelar kg/m³) ni #5 (mg/L vs % saturación) — siguen siendo preguntas exclusivas para el cliente, no resolubles por investigación.

### 7.8 Duración real de cada etapa y ración por temperatura (lectura completa del PDF FONDEPES, 2026-09-21)
El link de FONDEPES ahora se pudo leer completo (antes solo se había extraído por fragmentos con `pdftotext`). Esto permitió resolver con **cita directa, no inferencia**, cuánto dura la transición entre etapas — ver la tabla ya incorporada en la sección 3.2. Dos hallazgos adicionales de esta lectura completa:

**Tabla 6 del protocolo (Klontz, 1991) — ración por talla × temperatura del agua:** existe una tabla completa (peces de 3 a 32cm de longitud × agua de 4 a 13°C, en kg de alimento por 100kg de peces/día) que confirma con datos reales el principio señalado en la sección 7.6: a menor temperatura, la ración óptima baja sustancialmente para la misma talla. **No se transcribió aquí completa** porque la extracción automática del PDF tuvo ambigüedades en algunas celdas (dígitos poco legibles) — para no meter un dato mal transcrito en un documento que va a guiar decisiones de producción, la fuente de verdad es el PDF guardado en `docs/fuentes/fondepes-protocolo-engorde-trucha-2009.pdf` (Tabla 6, páginas XII-XIII del documento). Revisar ahí directamente antes de usar valores específicos de esta tabla.

**Ejemplo de "Programa de Producción" de FONDEPES (anexo del protocolo, para 5 TM de trucha)** — es el equivalente FONDEPES del Excel de Sierra Nevada: proyección mes a mes (1 a 12) con biomasa, truchas/kg, unidades vivas, talla, alimento, tipo de dieta, tasa alimenticia y **conversión (FCA) esperada por mes**, no solo por talla:

| Mes | Talla (cm) | Tasa alimenticia | FCA esperado | Estadio |
|---|---|---|---|---|
| 1 | 4 | 8% | 0.9 | Alevines I |
| 2 | 5.5 | 6% | 0.9 | Alevines II |
| 3 | 8 | 5% | 0.9 | Alevines II/III* |
| 4 | 10 | 3% | 0.9 | Alevines III |
| 5 | 12.5 | 2.5% | 1.0 | Juveniles I |
| 6 | 15 | 2% | 1.0 | Juveniles II |
| 7 | 17 | 2% | 1.1 | Engorde |
| 8 | 19 | 1.8% | 1.1 | Engorde |
| 9 | 21 | 1.5% | 1.1 | Engorde |
| 10 | 23 | 1.5% | 1.1 | Engorde |
| 11 | 25 | 1.5% | 1.1 | Engorde |
| 12 | 29 | 1.5% | 1.1 | Engorde |

*(el propio documento repite "Alevines II" en los meses 2 y 3 del ejemplo, probablemente porque el mes 3 cruza el límite Alevines II→III a mitad de mes — un detalle real de que las transiciones no caen limpio en el borde de un mes, algo a tener en cuenta si el nuevo sistema calcula "días exactos en cada etapa").* El ejemplo también reporta al final: FCA promedio real del ejemplo = 1.08, peso final ≈300g, y mortalidad explícitamente atada a "condiciones óptimas" (sección 7.3). Este ejemplo es útil como **segunda referencia cruzada** (independiente del Excel de la empresa) para validar o contrastar las tablas que se definan en el nuevo sistema.

---

## 8. Siguiente paso

Este documento es la base de conocimiento para rediseñar el módulo de producción en la migración a C#/React. Antes de escribir código, conviene resolver las preguntas restantes de la sección 6 con el cliente, y decidir si el nuevo sistema incorpora:
- Un **motor de proyección de campaña** (equivalente al Excel, con tablas por talla derivada de peso vía K) además del registro operativo diario que ya existe hoy en Django.
- Un **motor de captura y calibración**: registro real de muestreos (peso/talla), alimento entregado, mortalidad y fechas de transición de etapa por lote/jaula, para comparar proyectado vs. real y recalibrar progresivamente K/ración/FCA/mortalidad con datos propios de Sierra Nevada en vez de depender indefinidamente de tablas externas.
- Ajuste de la ración por **temperatura del agua**, no solo por talla (brecha detectada en sección 7.6; tabla real de referencia en `docs/fuentes/fondepes-protocolo-engorde-trucha-2009.pdf`, Tabla 6).
- Cálculo de oxígeno vía fórmula **Benson-Krause** en vez de mg/L crudos, para tener un umbral corregido por altitud real (4,500msnm).
- Registrar **fecha de inicio/fin de cada etapa por lote** para poder comparar la duración real (Alevinaje I-III, Juveniles I-II, Engorde I-II) contra la referencia FONDEPES de la sección 3.2 (~9 meses de ciclo total) — es el dato base que el usuario pidió poder calcular ("cuánto se demoró de alevines 1 a alevines 2 y cuánto creció en ese tiempo").
- Incorporar **medición/cálculo de densidad por jaula-artesa** (diseño detallado en 8.1) — hoy es una laguna real de control (sección 6, pregunta 4).

### 8.1 Diseño: cómo medir/calcular la densidad

La empresa **no controla densidad activamente hoy** (confirmado por el cliente, sección 6). No es algo que se mida con un instrumento — se calcula:

**Densidad (kg/m³) = Biomasa actual del lote / Volumen de la jaula o artesa**

**Volumen (m³):** dato fijo por unidad. Ya existe como campo en el modelo actual de Django (`UnidadProduccionBiomasa.volumen`) — hay que confirmar que esté poblado con el volumen real de cada jaula/artesa de Sierra Nevada, no un valor por defecto.

**Biomasa actual (kg):** esto sí requiere proceso de campo periódico. El protocolo FONDEPES (sección 8.1 del PDF, "Movimiento de Biomasa — Inventario") describe el método estándar de la industria:
- Suspender alimentación 24–48h antes (según temperatura del agua).
- Dividir la jaula en dos ambientes (vacío + con peces).
- Capturar con chinguillo, pesar con balanza tipo reloj, contar.
- 3 repeticiones (inicio/intermedio/final) para sacar peso promedio por pez.
- Biomasa = peso promedio × número de peces vivos.

No se hace a diario — es periódico, y coincide naturalmente con las **selecciones por talla** (que FONDEPES recomienda como labor constante desde alevinaje) y con la operación de **Selección** del propio protocolo, que es el mecanismo físico para bajar densidad cuando se acerca al límite (mover peces a otra jaula).

**Propuesta de diseño — dos capas, consistente con el motor de proyección/calibración ya definido:**
1. **Densidad medida:** se recalcula cada vez que se registra un inventario/muestreo real (biomasa real / volumen). Es el dato "de verdad".
2. **Densidad proyectada:** el motor de proyección (cadena día a día de biomasa, igual que el Excel) estima la densidad entre inventarios, para **alertar antes** de llegar al límite (13–15 kg/m³ de referencia FONDEPES, sección 3.1 — a recalibrar con datos propios de Sierra Nevada con el tiempo, no un valor fijo para siempre) sin depender de pesajes semanales.
3. Al registrarse un inventario real, corrige la proyección — mismo patrón proyectado-vs-real del resto del sistema.
4. El sistema debería **sugerir/alertar "selección recomendada"** cuando la densidad proyectada se acerque al umbral, no solo mostrar el número — conecta el cálculo con la acción operativa real (mover peces a otra jaula).

### 8.2 Progresión de 3 fases para el motor de producción (incluye visión de Machine Learning)

Se evaluó explícitamente si el motor de calibración debía construirse con Machine Learning desde el inicio. **Decisión: no.** Un ciclo de trucha dura ~9 meses, por lo que la empresa acumula pocos lotes por año — con ese volumen de historial, un modelo de ML se sobreajustaría en vez de aprender un patrón real. Además, dado que una falla en las recomendaciones de producción genera pérdidas económicas grandes, un modelo de caja negra sin explicabilidad es un riesgo innecesario en esta etapa. Se acordó una progresión de 3 fases:

1. **Motor de proyección de campaña** — equivalente al Excel actual, con tablas semilla validadas (FONDEPES + Excel de la empresa + decisiones de la sección 6).
2. **Motor de calibración estadístico/determinístico** — con datos propios capturados (K, FCA, mortalidad, duración por etapa, todos reales, por lote), comparados contra la referencia y usados para ajustar las tablas progresivamente (ej. promedios móviles, regresión simple por talla/temperatura). Transparente y auditable.
3. **Motor adaptativo con Machine Learning** — capa futura, viable solo una vez exista suficiente historial acumulado por la fase 2 (varios años/campañas). No se construye ahora, pero el modelo de datos de la fase 2 debe diseñarse pensando en que ese historial es, a la vez, el dataset de entrenamiento futuro de la fase 3.

### 8.2.1 Herramienta y stack técnico propuesto para la fase 3 (diseño documentado, no implementado)

**No existe, para crecimiento de trucha, un ecosistema de modelos pre-entrenados descargables y afinables** (el paradigma "tomar un checkpoint público + fine-tuning" que sí existe en visión/lenguaje). Lo "ya establecido" en acuicultura son **modelos bioenergéticos/mecanicistas** — ecuaciones de consumo, respiración y crecimiento parametrizadas por especie a partir de literatura fisiológica, no pesos entrenados. La referencia más citada es el **Fish Bioenergetics 4.0** (Deslauriers et al., 2017, *Fisheries*), que incluye el set de parámetros publicado para *Oncorhynchus mykiss* (trucha arcoíris). La propuesta es tomar esos parámetros de literatura como punto de partida y calibrarlos con los datos reales de Sierra Nevada (fase 2), en vez de partir de cero.

**Decisión de stack — quedarse en .NET, sin microservicio Python separado:**

| Opción evaluada | Descartada por |
|---|---|
| Microservicio Python (scikit-learn/PyTorch) llamado por HTTP desde el backend .NET | El VPS de Hostinger contratado tiene 2–4GB de RAM (sección 7) para correr .NET + PostgreSQL — ya se observaron problemas de memoria en desarrollo local con un solo runtime. Sumar un segundo runtime (Python + librerías de ML) duplica la huella de memoria y la superficie de despliegue/mantenimiento, sin necesidad real dado el tamaño del dataset. |
| Red neuronal (LSTM/Transformer) entrenada desde cero | Ya descartado en la decisión de sección 8.2 — con pocos lotes/año se sobreajusta. |

En cambio, todo el motor de fase 3 se implementa **dentro de la misma solución .NET** (capa `Domain`/`Application`, mismo patrón que el motor de proyección/calibración actual), en dos piezas:

1. **Capa mecanicista (bioenergética):** las ecuaciones de consumo/respiración/crecimiento del modelo publicado (Fish Bioenergetics, adaptado a trucha arcoíris) se implementan directamente como funciones C# — son fórmulas parametrizadas, no requieren un framework de ML. Los parámetros libres del modelo se ajustan (curve fitting simple, mínimos cuadrados) contra los muestreos reales acumulados por la fase 2, en vez de dejarlos en sus valores de literatura sin calibrar.
2. **Capa de actualización estadística (bayesiana):** para parámetros que la empresa necesita ir corrigiendo con cada campaña nueva (K, FCA, tasa de mortalidad por etapa), se usa **actualización bayesiana conjugada** — Normal-Normal para parámetros continuos (K, FCA) y Beta-Binomial para tasas (mortalidad como probabilidad), siguiendo Gelman et al. (*Bayesian Data Analysis*, cap. 2–3). Estas actualizaciones tienen forma cerrada (no requieren MCMC ni una librería pesada), funcionan bien con muestras pequeñas, y devuelven un intervalo de incertidumbre además del punto estimado — relevante porque una mala recomendación en producción cuesta caro (mismo argumento que motivó no usar caja negra en la sección 8.2). Se implementa como una clase de dominio más, junto a `CurvaCrecimientoReferencia`/`EtapaProductivaCalculadora`.

**Trabajo futuro fuera de alcance de la tesis (documentado, no implementado):** si en el futuro se acumulan varios años de historial y se necesita capturar interacciones más complejas entre variables (temperatura, densidad, oxígeno, genética del lote) que un modelo mecanicista + bayesiano simple no capture bien, la ruta natural es **ML.NET** (`Microsoft.ML`, específicamente `Microsoft.ML.Trainers.LightGbm` para gradient boosting sobre árboles) — se mantiene en el mismo runtime .NET, sin agregar Python al despliegue, y da modelos interpretables vía importancia de features. Solo se justificaría con un dataset bastante más grande del que la fase 2 puede producir en el horizonte de la tesis.
