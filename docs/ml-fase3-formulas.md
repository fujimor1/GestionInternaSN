# Fase 3 — Cómo funciona el motor adaptativo (ML)

Diseño documentado para la tesis, no implementado todavía (la decisión completa y sus fuentes están en `docs/investigacion-parametros-produccion.md`, sección 8.2/8.2.1). No se usa ninguna librería de Machine Learning ni microservicio externo: todo se implementa como funciones C# dentro del mismo backend .NET, junto a `CurvaCrecimientoReferencia`/`EtapaProductivaCalculadora`.

La idea completa se entiende mejor como un ciclo que se repite campaña tras campaña, no como piezas sueltas. Va así:

## Paso 1 — Proyectar el crecimiento de la campaña en curso

Todo lote parte con una expectativa de crecimiento. Hoy esa expectativa asume condiciones de agua constantes; la mejora de la fase 3 es meter la temperatura real del agua como variable, usando el modelo de Grados-Día (Thermal Growth Coefficient), estándar en acuicultura de salmónidos.

La lógica es: el peso crece más rápido cuando el agua está más caliente, y ese efecto se acumula día a día. Se resume en un solo número, el TGC, que se calcula así una vez termina una campaña:

    TGC = ( peso_final^(1/3) − peso_inicial^(1/3) ) / Σ(temperatura_diaria × días) × 1000

Y se usa al revés, como predicción, mientras la campaña está en curso:

    peso_final^(1/3) = peso_inicial^(1/3) + TGC × Σ(temperatura_diaria) / 1000

El exponente 1/3 no es una fórmula nueva sin relación con el resto del sistema — es la misma raíz cúbica que ya usa el Factor de Condición de Fulton (peso = K × talla³), aplicada ahora al peso total en vez de a la relación talla-peso.

El TGC arranca con un valor semilla (de FONDEPES o del Excel de la empresa). El paso 2 explica cómo ese número deja de ser fijo y empieza a moverse solo.

## Paso 2 — Corregir ese número con lo que realmente pasó

Cuando una campaña termina, el sistema ya tiene el TGC "de libro" (el que usó para proyectar) y el TGC "real" (el que se puede recalcular con los muestreos reales de esa campaña, con la misma fórmula del paso 1). En vez de simplemente reemplazar uno por el otro, se combinan con una regla estadística — la actualización bayesiana conjugada — que pondera cuánta confianza había en el valor de libro contra cuántos datos reales ya se acumularon.

Para un número continuo como el TGC (lo mismo aplica al Factor K o al FCA), la regla es Normal-Normal:

    Antes de esta campaña:  el valor creído es μ₀, con una incertidumbre σ₀²
    Esta campaña aportó:    el promedio x̄ de n muestreos reales

    El nuevo valor creído queda:
      μ_nuevo  = (σ² · μ₀ + n · σ₀² · x̄) / (σ² + n · σ₀²)
      σ²_nuevo = (σ² · σ₀²) / (σ² + n · σ₀²)

Para la mortalidad, que no es un número continuo sino una tasa (cuántos de cuántos murieron), la regla equivalente es Beta-Binomial:

    Antes de esta campaña:  se creía una tasa p, codificada como α₀ y β₀
                            (ej. α₀/β₀ tal que el promedio dé "3–5% esperado", el rango que reporta FONDEPES)
    Esta campaña aportó:    k muertes de n peces en esa etapa

    El nuevo valor creído queda:
      p_nuevo = (α₀ + k) / (α₀ + β₀ + n)

## Paso 3 — Ese resultado se convierte en el punto de partida de la siguiente campaña

μ_nuevo (o p_nuevo) no se guarda aparte: se convierte en el μ₀ (o α₀/β₀) con el que arranca el Paso 1 de la próxima campaña. Por eso el sistema no necesita reprocesar todo el historial cada vez que aprende algo nuevo — cada campaña solo necesita el resultado resumido de la anterior, no los datos crudos de todas las anteriores.

El efecto, visto a lo largo de varias campañas: al principio, con pocos datos propios acumulados, el número se queda pegado a lo que dice FONDEPES/Excel (porque σ₀²/α₀+β₀ todavía dominan la fórmula). Con cada campaña que se suma, el peso del promedio real (x̄ o k/n) crece, y el número converge hacia el comportamiento real de Sierra Nevada. Es, literalmente, la fórmula de la "recalibración progresiva" que se planteó como objetivo desde el inicio del proyecto.

## Por qué este mecanismo y no una red neuronal

Un ciclo de trucha dura ~9 meses, así que la empresa acumula pocas campañas por año — muy poco dato para entrenar una red desde cero sin que se sobreajuste. Las fórmulas de arriba están hechas justo para ese escenario: Normal-Normal y Beta-Binomial son estables incluso con pocas observaciones, porque el prior (el conocimiento previo de FONDEPES/Excel) sostiene la estimación mientras el dato propio todavía es escaso.

También importa la explicabilidad: como una recomendación equivocada en producción cuesta caro, cada resultado sale acompañado de su propia incertidumbre (σ²_nuevo, o el rango que da el Beta) — se puede mostrar y justificar el número frente a un jurado o al cliente, no es una caja negra.

Y en términos de implementación: todo esto son sumas, productos y una raíz cúbica — una función C# de pocas líneas por cada fórmula, sin agregar ninguna librería nueva ni un segundo servicio en Python.
