# Guión explicativo — Capítulo 1 (Planteamiento del Problema) y Capítulo 2 (Marco Teórico)

Este documento explica, punto por punto y sin siglas sin explicar, todo lo que dicen los capítulos 1 y 2 de la tesis. La idea es que sirva como guión para exponerlo en voz alta: cada sección está escrita como si se la estuviera contando a alguien que no sabe nada de sistemas, de acuicultura ni de estadística.

---

## La idea central, antes de entrar en el detalle

Antes de ir punto por punto, conviene tener clara la idea que sostiene toda la tesis, porque todo lo demás es una consecuencia de ella:

**En una piscigranja de truchas, el alimento balanceado (los sacos de pellets con los que se alimenta a los peces) es, de lejos, el gasto más grande del negocio — entre 6 y 7.5 de cada 10 soles que gasta la empresa se van en comprar ese alimento.** A pesar de ser el gasto más importante, hoy se maneja con cuadernos de papel: cuánto entra, cuánto sale, cuándo vence, cuánto queda. Nadie calcula con precisión cuánto darle de comer a los peces según cómo está el agua ese día, y nadie anticipa cuándo hay que volver a comprar antes de que se acabe el stock. Eso genera tres tipos de pérdida de dinero: se le da de más a los peces (se desperdicia comida y se ensucia el agua), se le da de menos (crecen más lento), o se compra tarde y se corta el plan de alimentación, o se compra de más y el alimento se vence guardado.

La tesis propone resolver esto con un sistema web que hace tres cosas al mismo tiempo: (1) lleva el control digital de cuánto alimento entra y sale, como un inventario ordenado; (2) usa un modelo matemático que aprende de los propios datos de la granja para calcular cuánto darle de comer a los peces cada día, y cuándo conviene volver a comprar alimento antes de quedarse sin stock; y (3) sigue funcionando aunque no haya señal de internet en las pozas, porque ahí es donde realmente se toman los datos. Esa combinación de las tres cosas — inventario, modelo que aprende, y funcionamiento sin internet — es, en una frase, de qué trata la tesis completa.

Todo lo que sigue es el desarrollo detallado de esa misma idea.

---

## Capítulo 1 — Planteamiento del Problema

### 1.1 Descripción del problema

Este punto explica, de lo general a lo particular, por qué este problema importa.

Primero se ubica el tema en el mundo: la crianza de peces y otros animales acuáticos (la acuicultura) es una actividad que crece rápido en el planeta, y desde hace años produce más alimento para las personas que la pesca tradicional en el mar o en ríos. Eso la vuelve importante para alimentar a la población mundial.

Pero dentro de esa actividad hay un problema recurrente en cualquier país: administrar bien el alimento balanceado cuesta trabajo, y como ya se dijo, representa la mayor parte del gasto de la empresa. El alimento además se puede echar a perder si se guarda mal o por mucho tiempo, así que no es solo un tema de gastar dinero, sino de gastarlo bien y a tiempo.

Luego se baja al Perú: aquí la crianza de trucha es la actividad acuícola más importante del país, con una cosecha anual que supera las 38,000 toneladas. El problema es que más del 90% de los criaderos de trucha del país son negocios pequeños o con recursos limitados, y casi ninguno tiene un sistema digital — todo se anota a mano, sin estandarizar.

Finalmente se baja al caso concreto de esta tesis: una empresa de truchas ubicada en Huaral (Lima), donde se hizo un diagnóstico real visitando las instalaciones. Ahí se encontraron cinco problemas concretos, que conviene explicar uno por uno porque son la base de todo lo demás:

1. **El inventario de alimento se lleva en cuadernos de papel, no en un sistema.** Cuando llega un camión con sacos de alimento, se anota a mano quién lo trajo, cuándo vence, y qué tamaño de pellet (bolita de alimento) es. Cuando se saca alimento para darle de comer a los peces, también se anota a mano. El problema es que nadie puede saber, en el momento, cuánto alimento hay realmente guardado — solo se sabe cuando alguien se sienta a sumar las libretas, y para entonces ya pasó tiempo y pueden haber errores.

2. **No hay ninguna herramienta que calcule con precisión cuánto darle de comer a los peces cada día.** Hoy esa decisión la toma el trabajador a ojo, o usando una tabla genérica que viene con el alimento comprado. El problema de esas tablas genéricas es que no toman en cuenta cómo está el agua ese día — si está más fría o más caliente, si tiene menos oxígeno disuelto — y esos factores sí afectan cuánta hambre tiene el pez. El resultado es que a veces se le da de más (se desperdicia comida y se contamina el agua) y a veces de menos (el pez crece más lento de lo que podría).

3. **No hay forma de anticipar cuándo hay que volver a comprar alimento.** Como no existe un cálculo que proyecte cuánto alimento se va a necesitar en las próximas semanas según cuántos peces hay y cuánto están creciendo, las compras se hacen de manera reactiva: cuando ya casi no queda, o por las dudas se compra de más "para no quedarse sin". Ambas cosas salen caras: quedarse sin alimento corta el plan alimenticio de los peces, y comprar de más hace que el alimento se quede guardado por semanas perdiendo calidad.

4. **En las pozas donde están los peces no hay señal de celular ni de wifi.** Esto es clave para entender por qué el sistema no puede ser un sistema web común: si el trabajador mide el peso de los peces o anota que murieron algunos, y el sistema necesita internet para guardar ese dato, esa información se pierde o queda pendiente hasta que la persona regresa a la oficina — con el riesgo de olvidarla o anotarla mal de memoria.

5. **Armar los reportes para la gerencia toma muchas horas de cálculo manual**, tiempo que se le quita a las tareas del día a día con los peces, y además retrasa la emisión de boletas y guías cuando se vende la cosecha.

La conclusión de esta sección es que estos cinco problemas, juntos, justifican construir un sistema web con tres capacidades: automatizar el control del inventario, calcular la ración diaria óptima usando un modelo que aprende de datos reales del agua y de los peces, y avisar a tiempo cuándo conviene comprar más alimento — todo evaluado después con rigor para confirmar que realmente funciona.

### 1.2 Formulación del problema

Aquí el problema, ya descrito en detalle en el punto anterior, se convierte en preguntas concretas que la tesis va a responder.

**La pregunta general** es: ¿de qué manera construir este sistema web (que usa un modelo que aprende de los datos) ayuda a mejorar el manejo del inventario de alimento en esta empresa de truchas?

Y se divide en **tres preguntas específicas**, cada una enfocada en una parte distinta del sistema:

1. ¿La parte funcional del sistema (o sea, que efectivamente registre entradas y salidas, calcule saldos, etc.) ayuda a controlar mejor el stock de alimento?
2. ¿La parte que predice — el modelo matemático que calcula cuánto alimento hará falta — ayuda a planificar mejor cuándo comprar?
3. ¿Que el sistema sea fácil de usar ayuda a que el personal trabaje de forma más eficiente en el día a día?

Estas tres preguntas no son arbitrarias: cada una corresponde a una de las tres capacidades que se identificaron como necesarias en el punto 1.1 (control de inventario, predicción, y facilidad de uso en campo).

### 1.3 Objetivos de la investigación

Los objetivos son, literalmente, las mismas tres preguntas del punto anterior pero convertidas en afirmaciones de lo que se va a hacer, en vez de preguntas.

**El objetivo general** es: construir el sistema web con el modelo que aprende de los datos, para mejorar el manejo del inventario de alimento en esta empresa.

**Los tres objetivos específicos**, uno por cada pregunta específica:

1. Determinar si la parte funcional del sistema mejora el control del stock.
2. Evaluar si la capacidad de predicción del modelo mejora la planificación de las compras.
3. Analizar si la facilidad de uso del sistema mejora la eficiencia con la que trabaja el personal.

Esta correspondencia exacta entre pregunta y objetivo es intencional y es justo lo que se espera que un jurado revise: que cada objetivo específico responda a una pregunta específica, y que al final del trabajo se pueda decir, con datos, si cada uno se cumplió o no.

### 1.4 Delimitación de la investigación

Esta sección responde tres preguntas simples: dónde, cuándo, y sobre qué exactamente se investiga — para dejar claro qué SÍ entra en la tesis y qué no.

- **Dónde (delimitación espacial):** en las oficinas, almacenes y pozas de cultivo de la empresa de truchas en Huaral, Lima.
- **Cuándo (delimitación temporal):** durante el año 2026, cubriendo desde entender qué necesita la empresa, diseñar el sistema, construirlo por partes, entrenar y ajustar el modelo matemático, ponerlo en un servidor en internet, capacitar a los trabajadores, y finalmente medir si funcionó.
- **Sobre qué exactamente (delimitación temática y tecnológica):** la tesis se ubica dentro de las líneas de investigación de Ingeniería de Software y de Inteligencia Artificial Aplicada de la universidad. En términos técnicos, se limita a construir una aplicación web con una arquitectura de servidor moderna, que funcione como aplicación web progresiva (es decir, que se pueda usar como si fuera una aplicación instalada, incluso sin internet), con una base de datos relacional, y con los dos mecanismos matemáticos de aprendizaje que se explican más adelante en el marco teórico.

### 1.5 Justificación del problema

Aquí se explica, desde seis ángulos distintos, por qué vale la pena hacer esta investigación. Conviene explicar cada uno con un ejemplo simple:

1. **Justificación teórica:** la tesis no solo construye un sistema, también aporta conocimiento — demuestra que es posible resolver un problema de inventario cuando la cantidad futura que se necesita depende de factores que cambian todos los días (como la temperatura del agua), usando un modelo estadístico que no necesita miles de datos para funcionar bien.

2. **Justificación práctica:** resuelve problemas concretos y cotidianos de las personas involucradas — el trabajador de campo puede anotar datos sin necesitar señal, el encargado de producción recibe una sugerencia calculada (no una adivinanza) de cuánto alimento dar, y la administración tiene un inventario digital que evita quedarse sin stock o perder dinero por alimento vencido.

3. **Justificación tecnológica y de innovación:** se explica que se está integrando un conjunto de tecnologías modernas y coherentes entre sí (un servidor bien organizado, un modelo matemático que corre muy rápido dentro del mismo programa sin depender de servicios externos, una aplicación de página que responde rápido, y capacidad de seguir funcionando sin internet).

4. **Justificación metodológica:** se explica que la forma de trabajar en el proyecto combina una metodología ágil de desarrollo de software (que organiza el trabajo en ciclos cortos) con una metodología estándar para proyectos de datos, además de usar cuestionarios y escalas ya validadas por otros investigadores para medir si el sistema es fácil de usar, en vez de inventar una forma de medir propia.

5. **Justificación económica y financiera:** como el alimento es el gasto más grande de la empresa, cualquier mejora en cómo se dosifica y se compra se refleja directamente en menos gasto — menos desperdicio, menos alimento vencido, y menos compras de emergencia a último momento (que suelen costar más).

6. **Justificación ambiental y ecológica:** dar la cantidad exacta de alimento (ni de más ni de menos) reduce la cantidad de comida que cae al fondo de la poza sin ser comida, lo cual reduce la contaminación del agua que después se devuelve al río.

---

## Capítulo 2 — Marco Teórico

### 2.1 Antecedentes de la investigación

Los "antecedentes" son, en términos simples, otras tesis o investigaciones ya publicadas que tocan temas parecidos, y que sirven para apoyarse en lo que otros ya descubrieron en vez de partir de cero. Se organizan en tres niveles, de lo más general a lo más parecido al tema exacto de esta tesis:

- **Nivel general:** investigaciones sobre manejo de inventarios y sistemas de información en cualquier tipo de empresa (no necesariamente de peces).
- **Nivel intermedio:** investigaciones sobre cómo construir sistemas web modernos, aplicaciones que funcionan sin internet, y modelos que aprenden de los datos — pero todavía no específicamente sobre peces.
- **Nivel específico:** investigaciones que sí son directamente sobre crianza de peces, trucha arcoíris, calidad del agua, y predicción de cuánto alimento darles.

Dentro de cada nivel se separa además entre investigaciones hechas en otros países e investigaciones hechas en el Perú, para mostrar que el problema se ha estudiado tanto afuera como dentro del país.

Vale la pena explicar, en el guion, dos o tres ejemplos concretos de cada nivel en vez de listarlos todos (para no volverlo una lectura plana):

- En el nivel general, por ejemplo, una investigación doctoral de Estados Unidos (Cornell/Stanford) comparó métodos clásicos de pronóstico contra modelos de aprendizaje automático para predecir la demanda de un inventario, y encontró que los modelos de aprendizaje redujeron el error de predicción en casi 40% — esto respalda la idea de usar aprendizaje automático en vez de fórmulas fijas cuando la demanda depende de factores que cambian.

- En el nivel intermedio, una tesis de la Universidad Nacional Autónoma de México probó que guardar los datos localmente en el navegador y sincronizarlos después, cuando vuelve la señal, garantiza que no se pierda ningún dato capturado en el campo — esto es exactamente la técnica que se usa para que los trabajadores puedan anotar datos en las pozas sin señal.

- En el nivel específico, una tesis doctoral española logró bajar el Factor de Conversión Alimenticia (que mide cuántos kilos de alimento hacen falta para producir un kilo de pez) correlacionando la cantidad de oxígeno y la temperatura del agua con la cantidad real de alimento consumido — esto confirma que hay una relación real y medible entre las condiciones del agua y cuánto hay que alimentar, que es la misma relación que esta tesis busca aprovechar.

**Sobre la cantidad de referencias usadas:** en total el documento cita **38 fuentes bibliográficas**. De esas, **30 son antecedentes** — investigaciones o tesis previas, repartidas de forma pareja entre los tres niveles explicados arriba (5 investigaciones internacionales y 5 nacionales en el nivel general, 5 internacionales y 4 nacionales en el nivel intermedio, 5 internacionales y 6 nacionales en el nivel específico). Las **8 restantes** son fuentes teóricas y técnicas de respaldo: libros y artículos que sustentan los modelos matemáticos y de arquitectura de software que se explican en la sección 2.2, más dos fuentes de estadísticas oficiales (una de la organización de la ONU para la alimentación y la agricultura, y otra del Ministerio de la Producción del Perú).

Vale la pena mencionar, al exponerlo, dos cosas que hablan bien del trabajo: primero, que los antecedentes vienen de **más de una decena de países distintos** (Perú, España, Colombia, Argentina, México, Noruega, Ecuador, Islandia, Chile, China, Brasil, Estados Unidos), lo cual muestra que el problema no es exclusivo de esta empresa ni de este país. Segundo, que los antecedentes incluyen **desde tesis de pregrado hasta disertaciones doctorales**, lo que muestra que se revisó el tema con distintos niveles de profundidad académica, no solo trabajos superficiales.

### 2.2 Bases teóricas y fundamentación tecnológica

Esta sección explica, una por una, las piezas técnicas con las que se construye el sistema, y por qué se eligió cada una. Conviene explicarlas en el orden en que un dato viaja por el sistema: primero cómo se organiza el código del servidor, luego cómo se ve y se usa desde la pantalla, luego qué pasa cuando no hay internet, luego dónde se guardan los datos, luego cómo se calculan las predicciones, luego qué modelos vienen de la ciencia de la crianza de peces, y finalmente dónde vive todo esto en internet.

**2.2.1 — Cómo se organiza el código del servidor (arquitectura limpia).** El servidor no se escribe como un solo bloque de código desordenado, sino en capas separadas, cada una con una responsabilidad clara: una capa central que solo contiene las reglas del negocio (por ejemplo, qué es un lote de peces, qué es una campaña de siembra, cómo se calcula el crecimiento) sin depender de ninguna base de datos ni de internet; una capa que organiza las acciones que se pueden hacer (registrar un muestreo, registrar una compra de alimento); una capa que se conecta de verdad con la base de datos; y una capa final que expone todo eso a través de internet de forma seleccionable según el rol de la persona que entra al sistema (administrador, encargado de producción, u operario).

**2.2.2 — Cómo se ve y se usa desde la pantalla (frontend reactivo).** La pantalla que ve el usuario se construye con una tecnología que actualiza solo las partes de la pantalla que cambian, en vez de recargar toda la página cada vez — esto la hace sentir rápida y fluida incluso cuando se están mostrando datos que cambian todo el tiempo, como el peso de los peces o el stock disponible. Además, el código de la pantalla se organiza en módulos independientes por tema (producción, inventario, comercialización), para que sea más fácil de mantener con el tiempo.

**2.2.3 — Qué pasa cuando no hay internet (estrategia sin conexión primero).** Como ya se explicó en el problema, en las pozas de cultivo no hay señal. Para resolver esto, la aplicación guarda una copia de sí misma dentro del propio navegador del celular o la tableta, de forma que puede abrirse y usarse aunque no haya conexión. Cuando el trabajador registra un dato sin señal, ese dato se guarda primero en una base de datos pequeña que vive dentro del navegador, en una fila de espera ordenada por el momento exacto en que se tomó el dato. En cuanto vuelve la señal, el sistema envía automáticamente esa fila de datos pendientes hacia el servidor central, sin que el trabajador tenga que hacer nada más.

**2.2.4 — Dónde se guardan los datos (motor de base de datos).** Se usa un motor de base de datos que garantiza que ninguna operación quede a medias — por ejemplo, que si dos personas registran una salida de alimento al mismo tiempo, el sistema no se confunda y calcule mal el saldo restante.

**2.2.5 — Cómo se calculan las predicciones (motor de aprendizaje adaptativo).** Esta es la parte de Machine Learning de la tesis, y merece explicarse con cuidado porque es distinta a lo que la gente suele imaginar cuando escucha ese término. No se usa una red neuronal entrenada con miles de ejemplos (como las que reconocen caras o generan texto), porque aquí no hay miles de ejemplos: un ciclo completo de crianza de una trucha dura entre 8 y 9 meses, así que la empresa solo termina unos pocos lotes por año. Entrenar un modelo grande con tan poca información haría que el modelo memorice esos pocos casos en vez de aprender un patrón real, y además sería difícil explicar por qué el modelo dice lo que dice — algo riesgoso cuando una mala recomendación puede hacer perder dinero real.

En su lugar se usan dos mecanismos matemáticos simples y verificables, calculados directamente dentro del mismo servidor, sin depender de un programa externo:

- El primero es un modelo que calcula cuánto debería estar creciendo un pez según la temperatura acumulada del agua día a día (entre más calor acumulado, más rápido crece, dentro de un rango razonable). Este modelo resume el crecimiento esperado en un solo número, que se recalcula cada vez que termina una campaña de siembra.

- El segundo mecanismo es el que hace que el sistema "aprenda": cada vez que termina una campaña, el sistema compara lo que esperaba (según el modelo anterior) contra lo que realmente pasó, y ajusta su creencia usando una regla estadística clásica llamada actualización bayesiana. En palabras simples: el sistema empieza confiando en tablas de referencia ya publicadas (de instituciones del sector pesquero peruano), pero con cada campaña real que se completa, va corrigiendo poco a poco esos números hacia lo que realmente ocurre en esta granja en particular — sin necesitar volver a analizar todo el historial completo cada vez, solo el resultado resumido de la campaña anterior.

**2.2.6 — Modelos que vienen de la ciencia de la crianza de peces (modelos zootécnicos y logísticos).** Aquí se listan las fórmulas concretas que usa el sistema: cómo se calcula la ración diaria de alimento a partir del peso total de los peces vivos; cómo se calcula el punto en el que hay que volver a pedir alimento antes de quedarse sin stock, tomando en cuenta que el camión desde Lima hasta Huaral tarda entre 5 y 7 días en llegar; y cómo se calcula la cantidad óptima de alimento a pedir de una sola vez, para no pagar de más en fletes ni gastar de más en almacenamiento.

**2.2.7 — Dónde vive todo esto en internet (infraestructura en la nube).** Finalmente, se explica que todo el sistema corre en un servidor alquilado en internet, preparado para estar disponible casi todo el tiempo, con un candado de seguridad digital que cifra toda la información que viaja entre el navegador del usuario y el servidor.

---

## Resumen para cerrar la exposición

Si hay que resumir todo esto en una idea final para cerrar la exposición: la tesis identifica un problema real y medible (el alimento es el gasto más grande de la empresa y se maneja sin ninguna herramienta digital), lo respalda con 38 fuentes que muestran que el problema y sus posibles soluciones ya se han estudiado en más de diez países y con distintos niveles de profundidad académica, y propone una solución concreta y explicable — no una caja negra — que combina inventario digital, un modelo estadístico simple que aprende con cada campaña, y la capacidad de seguir funcionando sin internet en el campo.
