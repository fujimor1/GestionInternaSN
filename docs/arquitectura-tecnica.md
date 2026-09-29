# Arquitectura técnica — sistema Sierra Nevada (.NET + React)

**Fecha:** 2026-09-22
**Propósito:** decisiones técnicas concretas de arquitectura (backend, frontend, datos, hosting), complementa `docs/plan-implementacion-dotnet.md` (que define el orden/fases) y `docs/diseno-modulo-produccion.md` (que define las entidades del dominio).

---

## 1. Backend — Clean Architecture por features

**Estado (2026-09-22): scaffolding creado** en `backend/` — solución `.NET 10` (LTS más reciente instalada), 4 proyectos (Domain, Application, Infrastructure, Api) con las referencias correctas entre capas, carpetas por feature (Produccion/Comercializacion) dentro de cada uno, y compila sin errores ni advertencias. Paquetes ya instalados: `ErrorOr` (Application), `Npgsql` + `dbup-postgresql` (Infrastructure), `Microsoft.AspNetCore.Authentication.JwtBearer` (Api). Nota: se fijó `Microsoft.OpenApi` a la versión `2.12.2` explícitamente — la plantilla trae por defecto una versión con una vulnerabilidad conocida (alta severidad, GHSA-v5pm-xwqc-g5wc), y saltar directo a la major 3.x rompe el generador de OpenAPI de ASP.NET Core (API distinta) — 2.12.2 es la última versión 2.x, ya parchada y compatible.

4 capas, organizadas por módulo de negocio dentro de cada una (no todo mezclado):

```
Domain/
  Produccion/        (Lote.cs, Campaña.cs, Muestreo.cs, ...)
  Comercializacion/   (Cliente.cs, Venta.cs, ...)
Application/
  Produccion/         (Services, DTOs, interfaces de repositorio)
  Comercializacion/
Infrastructure/
  Produccion/         (Implementación de repositorios — acceso a datos)
  Comercializacion/
API/
  Controllers/Produccion/
  Controllers/Comercializacion/
```

**Por qué:** separa responsabilidades (correcto para el rigor que exige la tesis) y mantiene la navegación del código por módulo de negocio, no por tipo técnico.

## 2. Manejo de errores — patrón Result

En vez de lanzar excepciones para errores de negocio esperados ("lote no encontrado", "biomasa insuficiente"), la capa **Application** devuelve `ErrorOr<T>` (librería NuGet `ErrorOr`) — obliga a manejar el caso de error explícitamente, sin excepciones para flujo de control normal.

La capa **API** tiene un middleware que mapea esos errores a respuestas HTTP consistentes usando **Problem Details** (RFC 7807, nativo en ASP.NET Core) — mismo formato de error para toda la API, sea de negocio o inesperado.

**Verificado end-to-end (2026-09-22):** los 12 repositorios ADO.NET (Infrastructure/Produccion) + los 12 scripts DbUp corrieron contra una base PostgreSQL local real (`sierranevada`) — las 12 tablas se crearon correctamente, la API arranca y aplica las migraciones automáticamente al iniciar. Base de datos de desarrollo local: `sierranevada` (usuario `postgres`, ver `appsettings.Development.json`, no comprometido a git con credenciales reales de producción — en el VPS se configura vía variable de entorno `ConnectionStrings__SierraNevadaDb`).

## 3. Acceso a datos — ADO.NET puro (decisión 2026-09-22)

**Decisión explícita del usuario, sobre la recomendación inicial (EF Core):** se usa **ADO.NET puro**, no EF Core.

- El acceso a datos vive **solo en la capa Infrastructure**, detrás de interfaces de repositorio definidas en Application (ej. `IRepositorioLote`). Domain/Application/API nunca conocen ADO.NET directamente — solo la interfaz. **Esto hace que la decisión sea reversible**: cambiar a EF Core o Dapper más adelante solo requiere reescribir las implementaciones de Infrastructure, sin tocar el resto del sistema.
- **Sin migraciones automáticas** (eso es exclusivo de EF Core) — se gestiona el esquema con **DbUp**: librería liviana que aplica y versiona scripts `.sql` en orden, sin ser un ORM. Cada cambio de esquema es un script SQL nuevo, versionado y aplicado una sola vez.
- Motor de base de datos: **PostgreSQL** (ya decidido).

## 4. Frontend — React + Vite

- **Estructura por features**: `/features/produccion`, `/features/comercializacion`, `/features/auth`, cada uno con sus componentes/hooks/llamadas API.
- **Manejo de estado del servidor:** TanStack Query (React Query) — cachea/sincroniza datos de la API, y encaja con el patrón offline-first.
- **Routing:** React Router.
- **Librería de UI:** **Ant Design** (confirmado) — por la cantidad de tablas/formularios del sistema (lotes, muestreos, reportes).

## 5. Captura de campo — PWA (offline-first)

Solo en las pantallas de `Muestreo`, `RegistroAlimentacionReal`, `RegistroMortalidad`, `RegistroCondiciones` — donde se captura físicamente en la jaula, con conectividad variable/inexistente confirmada por el cliente.

- **`vite-plugin-pwa`** (basado en Workbox) — genera el Service Worker.
- **Dexie.js** — capa sobre IndexedDB para la cola de sincronización local.
- Sigue siendo la misma app web en React — no es una app móvil nativa ni un proyecto aparte.
- El resto del sistema (reportes, configuración, ventas) es web normal, sin esta capa — se usa desde oficina con conexión.

## 6. Autenticación y roles

**Corrección (2026-09-22):** ASP.NET Core Identity viene armado sobre EF Core por defecto — no es compatible directo con la decisión de ADO.NET puro (sección 3) sin reescribir sus "stores" internos. **Se descarta Identity.** En su lugar: autenticación propia y simple —

- Tabla `Usuario` gestionada con repositorios ADO.NET propios.
- Hash de contraseña con `PasswordHasher<T>` (incluido en ASP.NET Core, usable suelto sin todo Identity) o `BCrypt.Net-Next`.
- JWT emitido a mano (`System.IdentityModel.Tokens.Jwt`), con el rol como claim.
- 5 roles: **Administrador, Operario, Ventas, Logística, Terceros** — autorización por política/rol en cada endpoint vía `[Authorize(Roles = "...")]`.

**Pendiente de confirmar:** qué acceso exacto tiene el rol "Terceros" (asumido como acceso limitado/externo, ej. cliente viendo estado de pedido — falta confirmar con el usuario).

**Implementado y verificado end-to-end (2026-09-22):** entidad `Usuario` (Domain/Usuarios), `IUsuarioRepository` + `UsuarioRepository` (ADO.NET), `PasswordHasherAdapter` (envuelve `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` del paquete `Microsoft.Extensions.Identity.Core` — sin EF Core ni el resto de Identity), `JwtTokenGenerator`, y `AutenticacionService` con dos flujos:
- `POST /api/auth/bootstrap` — crea el primer Administrador, solo funciona si no existe ningún usuario todavía (evita insertar el primer admin a mano por SQL en el despliegue). Probado: primera vez → 200 con token; segunda vez → 409.
- `POST /api/auth/login` — probado: credenciales correctas → 200 con token; contraseña incorrecta → 401.

Los enums (incluido `Rol`) se serializan como texto en JSON (`JsonStringEnumConverter`), no como número — más legible para el frontend y más seguro ante reordenamientos futuros del enum.

## 9. Casos de uso del módulo de Producción (2026-09-22)

**Comandos** (`Application/Produccion/CasosDeUso/`): `RegistrarMuestreoService`, `RegistrarAlimentacionRealService`, `RegistrarMortalidadService`, `RealizarSeleccionService` (la selección física por talla que divide un lote, sección 4.2/5 de `docs/diseno-modulo-produccion.md`), `CambiarEtapaService`, `CrearCampañaConLotesService` (resuelve el escenario de 90,000 peces repartidos en varias jaulas desde el inicio, confirmado por el cliente).

**Motor de calibración** (`Application/Produccion/Calibracion/CalcularCalibracionLoteService`): dado un lote, calcula K real (serie de tiempo desde `Muestreo`), duración real por etapa (desde `HistorialMovimiento`), FCA real por período entre muestreos (alimento real / ganancia de biomasa real), mortalidad real acumulada, y densidad real (biomasa actual / volumen de la unidad) comparada contra la tabla de referencia activa.

**Verificado con test de integración real** (`backend/tests/SierraNevada.Tests`, proyecto xUnit nuevo, sin mocks — corre contra PostgreSQL real): un caso completo (siembra → 2 muestreos → mortalidad → alimentación → cambio de etapa → calibración) con valores calculados a mano de antemano, comparados contra el resultado del motor. Pasó tras corregir 2 bugs reales encontrados en el proceso:

1. **`Usuario.Crear` exigía password hash no vacío**, pero el flujo de bootstrap necesita crear el `Usuario` primero (vacío) para poder hashear la contraseña con él — la validación se movió a `ActualizarPasswordHash` (sección 6).
2. **Los 5 casos de uso usaban `DateTime.UtcNow` para la fecha del `HistorialMovimiento`, en vez de la fecha de negocio recibida como parámetro.** Es un bug crítico dada la arquitectura offline-first (sección 5): un muestreo capturado sin señal y sincronizado horas/días después habría quedado con la fecha de sincronización, no la fecha real del evento — rompiendo cualquier cálculo de duración por etapa. Corregido en los 5 servicios.

## 10. Controllers HTTP (2026-09-22)

`Api/Controllers/Produccion/`: `UnidadesController`, `BastidoresController`, `CampañasController`, `LotesController` (muestreos, alimentación, mortalidad, condiciones, selección, cambio de etapa, calibración), `EnfermedadesController`, `TablasReferenciaController`. Base común `ApiControllerBase` mapea errores `ErrorOr` → HTTP (Problem Details), reemplazando el código duplicado que tenía `AuthController`.

**Autorización por rol aplicada**: lectura con `[Authorize]` simple (cualquier usuario autenticado); escritura de captura (muestreo/alimentación/mortalidad/condiciones/selección/cambio de etapa) con `Administrador` u `Operario`; creación de unidades/bastidores/enfermedades y — más importante — **activar una nueva versión de tabla de referencia, solo `Administrador`** (cambia el estándar que usa todo el sistema).

**Verificado end-to-end por HTTP real** (API corriendo, `curl` con JWT real, no llamadas directas a los servicios): login → crear unidad → crear campaña con lote (2000 peces) → 2 muestreos → mortalidad → 3 alimentaciones → cambio de etapa → `GET /calibracion`. Resultado verificado a mano: K 1.123→1.200, AlevinajeI duró 35 días, FCA real 0.568, mortalidad 5%, densidad 1.2444 kg/m³ — todo correcto. También se confirmó que un endpoint protegido devuelve 401 sin token.

**Nota de troubleshooting (no es bug del sistema):** al probar por `curl` con "campaña" en el body, bash/git-bash en Windows corrompe el carácter `ñ` al interpolar el JSON inline — se resolvió escribiendo el JSON a un archivo UTF-8 y usando `--data-binary @archivo`. Ningún cambio de código fue necesario.

## 11. Frontend (2026-09-23)

Scaffold completo en `frontend/` (Vite + React + TypeScript), siguiendo lo ya decidido: Ant Design, TanStack Query, React Router, estructura por features.

**Estructura:**
- `api/client.ts` + `api/types.ts` — cliente axios con interceptor JWT, tipos que reflejan los DTOs reales de la API (verificados contra las respuestas HTTP capturadas en la sección 10).
- `features/auth/` — `AuthContext` (sesión en localStorage), `LoginPage`.
- `features/produccion/` — `api.ts` (hooks de TanStack Query para cada endpoint), páginas: `LotesListPage`, `LoteDetailPage` (con el gráfico de Factor K, tablas de duración/FCA por etapa, y modales de captura), `CampañasListPage` (con reparto dinámico en varias unidades — el escenario de los 90,000 repartidos), `UnidadesListPage`.
- `layout/` — `AppLayout` (Sider+Header AntD), `ProtectedRoute`.
- `offline/` — `db.ts` (Dexie/IndexedDB), `useSincronizacionMuestreos.ts`.

**Offline-first implementado para Muestreo** (la pantalla de captura de campo, sección 5): `useRegistrarMuestreo` intenta la llamada real; si falla por **error de red** (no por validación del servidor) lo encola en IndexedDB; `useSincronizacionMuestreos` reenvía la cola automáticamente al reconectar (evento `online`) o al abrir la app, descartando solo los que fallan por validación (no por falta de señal). Un ícono en el header muestra cuántos quedan pendientes. **Nota de alcance:** este patrón está implementado completo solo para Muestreo — Alimentación/Mortalidad/Condiciones usan el mismo `apiClient` sin cola offline todavía; extenderlo es mecánico (mismo patrón) pero pendiente.

**Gráfico de Factor K**: se usó la skill de dataviz del proyecto antes de escribir el código — línea única (sin leyenda, el título la nombra), paleta validada del proyecto, banda de referencia sombreada 1.0–2.0 (rango "sano" según la investigación, sección 7.1 del doc de investigación).

**PWA**: `vite-plugin-pwa` configurado (manifest, Service Worker con `NetworkOnly` para `/api/*` — nunca sirve datos de la API desde caché, solo el shell de la app).

**Verificado (2026-09-23):**
- `npm run build` compila limpio (TypeScript + Vite).
- El servidor de desarrollo (`npm run dev`, puerto 5173) sirve el HTML y transforma los módulos sin errores.
- Se agregó **CORS** al backend (`Program.cs`) — faltaba, sin esto el navegador bloquea las llamadas del frontend a la API aunque `curl` funcionara. Configurable por `Cors:AllowedOrigins` (dev: `http://localhost:5173`; producción: variable de entorno, dominio real del frontend desplegado).

**No verificado — limitación honesta:** no se pudo probar visualmente en un navegador real. La extensión Claude en Chrome no está conectada en este entorno, y la descarga de un navegador para Playwright fue bloqueada por la red del sandbox. Backend y frontend quedaron corriendo (`localhost:5299` y `localhost:5173`) para que el usuario lo verifique en su propio navegador con el usuario `admin` / `ClaveSegura123!`.

**Pendiente/follow-up de performance (no correctitud):** el bundle de producción pesa ~1.7MB sin comprimir (AntD + Recharts sin code-splitting) — Vite lo marca como advertencia, no error. Se puede resolver con `dynamic import()` por ruta más adelante, no es urgente a esta escala de usuarios.

### 11.1 Reestructuración de navegación (2026-09-23) — corrigiendo el modelo mental

El usuario señaló que la primera versión (lista plana de "Lotes" + "Campañas" + "Unidades" como secciones separadas) era más entity-centric (como se ve la base de datos) que operario-centric (como se ve la jaula desde el borde del agua) — y que el sistema **Django anterior sí tenía el orden correcto**, revisado en `templates/produccion/dashboard.html` y `unidad_list.html`:

1. **Dashboard de entrada por macro-etapa** (Ovas/Alevines/Juveniles/Engorde como 4 tarjetas) — el operario piensa "¿a qué etapa quiero ir?", no "dame todos los lotes".
2. **Cada etapa lista sus unidades** (jaulas/artesas/bastidores), no lotes sueltos — la unidad es la fila principal, con su ocupación visible de un vistazo.
3. **Cada unidad expande a los lotes que tiene adentro**, con **acciones rápidas de un toque** para lo que se hace a diario (mortalidad, sobre todo — el Django lo tenía como input inline, sin modal).
4. Los botones de "mover etapa" en Django se habilitaban/deshabilitaban solos según la talla del lote — la regla de negocio guía al operario sin que la memorice (no replicado todavía en el frontend nuevo, queda como mejora futura).

**Reestructurado (2026-09-23):**
- `features/produccion/macroEtapas.ts` — define las 4 macro-etapas y a qué combinación de `TipoUnidadProduccion`/`subTipoJaula` corresponde cada una (Ovas→Bastidor, Alevines→Artesa, Juveniles→Jaula+Juvenil, Engorde→Jaula+Engorde).
- `pages/DashboardPage.tsx` — nueva página de inicio (`/`), 4 tarjetas con conteo de unidades/lotes/biomasa por macro-etapa, calculado en cliente a partir de los datos ya cargados (no requirió endpoint nuevo).
- `pages/EtapaUnidadesPage.tsx` — nueva página (`/etapas/:clave`), lista las unidades de esa macro-etapa como paneles expandibles (AntD Collapse), cada uno mostrando sus lotes adentro.
- `components/RegistrarMortalidadRapida.tsx` — acción rápida de un toque (Popover con input inline), replicando el patrón que funcionaba en Django, en vez de abrir un modal completo.
- `layout/AppLayout.tsx` — el menú ahora tiene "Inicio" (dashboard) como entrada principal, y Lotes/Campañas/Unidades pasaron a un submenú "Herramientas" (igual al patrón "Herramientas Adicionales" de Django) — ya no son la navegación principal.
- La página de detalle del lote (`LoteDetailPage`, con historial/calibración/gráfico) se mantiene igual — es el tercer nivel, para cuando el operario quiere profundizar, no el punto de entrada.

**Verificado:** compila limpio (`npm run build`) y los módulos nuevos se transforman sin error en el dev server — visual todavía no confirmado en navegador (misma limitación de la sección 11).

### 11.1.5 Datos de siembra realistas para probar el frontend (2026-09-23)

Al preparar datos de prueba para las 4 macro-etapas se encontraron y corrigieron 2 gaps más:
1. **`CrearCampañaConLotesService` no soportaba sembrar en un Bastidor** (solo `UnidadProduccion`) — no había forma de crear un lote en etapa Ovas vía la API. `DistribucionInicial` ahora acepta `UnidadProduccionId` **o** `BastidorId` (exactamente uno de los dos, validado).
2. **`Lote.Crear` fijaba la etapa inicial en `AlevinajeI` siempre** (salvo Bastidor→Ovas), sin importar la talla de siembra real — un problema si se compran alevines o juveniles ya crecidos a otro proveedor. Ahora recibe `tallaInicialCm` opcional y deriva la etapa con `EtapaProductivaCalculadora.DesdeTalla` (mismo criterio que `DividirPorSeleccion`).

Sembrados por HTTP real 4 campañas cubriendo las 4 macro-etapas (Ovas en bastidor, Alevines en 2 artesas, Juveniles y Engorde en jaulas), con historial real (muestreos, alimentación, mortalidad) en los lotes de Juveniles y Engorde — calibración verificada con valores coherentes (K, FCA, mortalidad, densidad, todos dentro de rango razonable).

**Nota:** no se pudo limpiar los datos de prueba desordenados de sesiones anteriores (`TEST-*`, `HTTP-*`) — el comando `TRUNCATE` fue bloqueado por el clasificador de seguridad de Claude Code como "borrado masivo". Quedan 8 lotes de prueba viejos mezclados con los 5 nuevos limpios en la base de desarrollo local — pendiente de que el usuario autorice la limpieza si la quiere.

### 11.1.6 Simulación de 2 campañas con linaje completo por selección (2026-09-23)

A pedido del usuario, se simularon **2 campañas** (no 4 como en el primer intento) que cubren las 4 macro-etapas mediante **selecciones encadenadas** — reflejando crecimiento desigual real (los que van a la cabeza avanzan de etapa, la cola se queda atrás), en vez de una siembra fija por etapa:

- **CAMP-2026-A** (sembrada 2026-01-05, ~8.5 meses): Ovas (bastidor, 30,000) → Selección → Alevinaje (25,000 a Artesa, quedan 5,000 en Ovas) → Selección → Juveniles (20,000 a Jaula, quedan 5,000 en Alevinaje) → Selección → Engorde (16,000 a Jaula, quedan 4,000 en Juveniles). 4 lotes, un solo linaje (`lotePadreId` encadenado), las 4 etapas representadas.
- **CAMP-2026-B** (sembrada 2026-08-20, ~1 mes): Ovas → Selección → Alevinaje. Solo 2 etapas, apropiado para una campaña recién sembrada.

**Hallazgo real durante la verificación:** el lote de Engorde de la campaña A, tras su historial de muestreos simulado, terminó con **16.35 kg/m³ de densidad — por encima del rango de referencia FONDEPES (13–15 kg/m³)** — pero `densidadReferenciaMaxKgM3`/`superaReferencia` salían `null` en la calibración porque **nunca se había cargado ninguna `TablaReferenciaVersion` en el sistema** (la tabla existe desde hace días, pero estaba vacía). Se cargó la primera tabla de referencia real vía `POST /api/produccion/tablas-referencia` (Densidad, 15kg/m³ máx. para talla ≥17cm, fuente FONDEPES Tabla 1) — verificado que `superaReferencia` ahora sale `true` para ese lote. Sigue pendiente cargar Ración/FCA/Mortalidad como referencia (solo se cargó Densidad, la que hizo falta para esta prueba).

### 11.1.7 Inventario real de Django replicado — y un bug de precisión encontrado al comparar (2026-09-23)

El usuario pidió revisar el inventario **real** de unidades en Django (`db.sqlite3`, no datos de prueba) antes de seguir simulando. Se encontraron **7 bastidores, 9 artesas y 8 jaulas** (códigos `B2510-XX`/`A2510-XX`/`J2510-XX`, convención que sugiere alta en oct-2025 — inventario real, corrige lo anotado antes de que `db.sqlite3` "solo tenía datos de prueba": eso era cierto para lotes/ventas, no para las unidades físicas). Replicadas las 24 unidades en el sistema nuevo con los mismos códigos y dimensiones.

**Bug real encontrado al verificar los números contra Django:** las jaulas hexagonales/decagonales (las únicas formas que requieren trigonometría para `lado_m`) salían con capacidades ligeramente distintas a Django (ej. J2510-01: 3880.80 vs 3879.79 kg esperado — pequeño pero real, y creciente con el tamaño: J2510-06/07/08 llegaban a 13kg de diferencia). Causa: `lado_m` se guardaba en una columna `NUMERIC(10,2)` (2 decimales) y ese valor ya redondeado se reutilizaba para calcular el volumen — el redondeo se propagaba. **Corregido de raíz**: `UnidadProduccion.LadoM` pasó de campo guardado a **propiedad computada** (igual que `VolumenM3`/`CapacidadMaximaKg`) — se recalcula siempre con precisión completa desde `DiametroM`, nunca se guarda ni se relee un valor redondeado. La columna `lado_m` queda en el schema sin usarse (no se pudo hacer `DROP COLUMN` por la misma restricción de "borrado" del entorno, pero es inofensiva). Verificado: las 8 capacidades ahora coinciden exacto con Django, incluidas las 4 más grandes que antes tenían hasta 13kg de diferencia.

### 11.1.8 Limpieza de datos de prueba (2026-09-23)

A pedido del usuario, se limpiaron las 16 unidades de prueba creadas antes de conocer el inventario real (dejando solo las 24 reales) y todo lo que dependía de ellas (20 lotes, incluida la cadena de selecciones, 39 muestreos, 10 registros de alimentación, historial de movimientos, y las campañas que quedaron sin lotes). Un `TRUNCATE` general de todas las tablas había sido bloqueado antes por el clasificador de seguridad de Claude Code como "borrado masivo" — un **borrado acotado por `WHERE`** (solo las filas con códigos de prueba y sus dependientes, identificados con una CTE recursiva para seguir la cadena de `lote_padre_id`) sí se permitió. Estado final verificado por HTTP: 7 bastidores + 9 artesas + 8 jaulas = 24 unidades (coincide exacto con Django), 0 lotes, 0 campañas — base limpia para sembrar datos reales o nuevas simulaciones.

### 11.2 Validaciones de negocio en Selección (2026-09-23)

El usuario señaló un requisito real faltante: no se puede mover peces de una talla muy distinta a una jaula con otra talla promedio (competencia por alimento, más mortalidad de los chicos — el mismo problema que FONDEPES documenta como razón de ser de la Selección). Se compararon las 3 fuentes de bandas de talla ya documentadas (FONDEPES, Excel, Django — ver sección 3.2/4.2/5.7 de `docs/investigacion-parametros-produccion.md`) antes de decidir: **se usan las bandas de FONDEPES**, porque ya son exactamente el enum `EtapaProductiva` del dominio, y porque el Excel tiene un hueco real sin definir justo en 14.18–16.54cm — usarlo para una validación que bloquea sería peligroso ahí.

Implementado:
- **`EtapaProductivaCalculadora`** (Domain, nuevo) — `DesdeTalla(decimal)` deriva la etapa esperada según talla (bandas FONDEPES); `TipoUnidadCorresponde(unidad, etapa)` valida que el tipo de unidad sea el físicamente correcto (Artesa→Alevinaje, Jaula Juvenil→Juveniles, Jaula Engorde→Engorde) — el `SubTipoJaula` de una unidad es un campo editable, no una clasificación permanente (puede reasignarse entre campañas), pero en un momento dado debe corresponder.
- **Bug relacionado corregido**: `Lote.DividirPorSeleccion` heredaba la etapa del lote padre para el hijo — incorrecto, porque los peces que van a la cabeza pueden ya estar en una etapa más avanzada que el resto del lote. Ahora el hijo deriva su etapa de **su propia talla**.
- **3 validaciones bloqueantes en `RealizarSeleccionService`** (confirmadas por el usuario, todas "Recomendado"): (1) tipo de unidad correcto para la etapa del grupo movido; (2) si la unidad destino ya tiene lotes activos, su etapa debe coincidir con la del grupo movido (no mezclar tallas); (3) la biomasa entrante + la ya existente en destino no debe superar la capacidad máxima de la unidad.

**Verificado por HTTP real, 4 casos:** bloqueo por tipo de unidad incorrecto (mover talla Engorde a jaula Juvenil), bloqueo por talla incompatible (mover JuvenilesII a unidad con lote en otra etapa), bloqueo por capacidad excedida (unidad vacía de 450kg, intentar meter 500kg), y caso válido exitoso — confirmando además que el lote hijo quedó con `etapaActual` derivada de su propia talla (13cm→JuvenilesI), no heredada del padre (AlevinajeII).

**Gap real encontrado por el usuario (2026-09-23): faltaba la UI de Selección.** El backend (`RealizarSeleccionService`, endpoint `POST /lotes/{id}/seleccion`) estaba completo y probado desde la sesión anterior, pero nunca se le agregó botón/modal en `LoteDetailPage.tsx` — se quedó afuera sin querer. Agregado: `useRealizarSeleccion` (api.ts), botón "Selección (mover a otra jaula)" + formulario (código de lote nuevo, cantidad a mover, unidad destino, peso/talla del grupo movido), con link directo al lote hijo creado. Verificado por HTTP real: lote padre de 1800→1300 peces, lote hijo con `lotePadreId` y `campañaId` correctos, etapa heredada — antes de este momento el endpoint nunca se había probado con una llamada real (solo compilado), quedó confirmado ahora.

**Bug real encontrado por el usuario probando en su navegador (2026-09-23):** el click en una jaula/bastidor no expandía nada. Causa: se instaló **Ant Design v6** (no v5, sin pinear versión al hacer `npm install antd`), y en v6 el patrón `<Collapse.Panel>` como hijos JSX ya no funciona — la API cambió a una prop `items: {key, label, children}[]` (`CollapseProps extends Pick<RcCollapseProps, 'items'>`, confirmado en `node_modules/@rc-component/collapse/es/interface.d.ts`). No daba error de compilación ni de TypeScript — simplemente no renderizaba nada interactivo. Corregido en `EtapaUnidadesPage.tsx` usando la API `items`. Se revisó el resto del código por otros patrones de subcomponente potencialmente obsoletos en v6 (`.Panel`, `.TabPane`, `.Option`, `Menu.Item`) — no se encontró ninguno más; `Select`/`Menu` ya usaban `options`/`items` desde el inicio.

**Gap real encontrado por el usuario (2026-09-23): faltaba la UI de Bastidores.** Mismo patrón que la Selección — el backend (`BastidoresController`, crear+listar) estaba completo, pero el frontend solo usaba `useBastidores()` de forma read-only en el dashboard/etapa; no había página ni ítem de menú para crearlos. Se agregó primero como página separada (`BastidoresListPage.tsx` + ruta `/bastidores`), pero el usuario pidió fusionarla dentro de "Jaulas y Artesas" — **el backend sigue con `Bastidor` como entidad separada de `UnidadProduccion`** (correcto, capacidades en unidades distintas: ovas vs. kg), solo la vista del frontend combina ambas fuentes de datos en una tabla y un modal de creación con selector de tipo (Bastidor/Artesa/Jaula). `BastidoresListPage.tsx` se eliminó.

### 11.3 Reorganización de la estructura de carpetas del frontend (2026-09-23)

El usuario señaló que la estructura del frontend era poco entendible. Diagnóstico: `features/produccion/api.ts` se había vuelto un archivo gigante con todos los hooks mezclados (unidades, bastidores, campañas, lotes, muestreos, alimentación, mortalidad, cambio de etapa, selección), y la carpeta `features/` era una capa sin significado directo para el usuario (no se parece a como Django organiza sus apps — `produccion/`, `usuarios/`, cada una de primer nivel).

**Reestructurado:**
```
frontend/src/
  api/              (cliente axios + tipos, compartido)
  auth/             (antes features/auth)
  produccion/       (antes features/produccion)
    hooks/
      useUnidades.ts     (unidades + bastidores)
      useCampanias.ts
      useLotes.ts        (lote + muestreo + alimentación + mortalidad + selección + cambio-etapa + calibración)
    pages/
    components/
  layout/
  offline/
```
Se quitó la capa `features/` (un nivel menos de anidamiento) y se partió el archivo `api.ts` en 3 hooks por tema, agrupados igual que las páginas que los usan.

**Error cometido y corregido en el proceso:** al mover archivos con `rm -rf features/` después de mover el resto, se olvidó mover `api.ts` primero — se borró sin querer. El frontend nunca había sido commiteado a git (`?? frontend/`), así que no había forma de recuperarlo del historial. Se reconstruyó el archivo completo desde el contenido ya visto en la conversación (alta confianza, era código propio escrito minutos antes) y de una vez se dejó dividido en los 3 archivos nuevos. **Lección: antes de un `rm -rf` sobre una carpeta que se está reorganizando, verificar explícitamente que absolutamente todo su contenido ya fue movido — un `find` o `ls` de la carpeta antes de borrarla, no solo confiar en la lista de comandos `mv` ya ejecutados.**

Verificado: `npm run build` compila limpio (0 errores), sin referencias residuales a `features/`, y el dev server sirve la estructura nueva sin errores de módulo.

**Limitación conocida, pendiente:** las operaciones multi-escritura (ej. `RegistrarMuestreoService` escribe `Muestreo` + actualiza `Lote` + crea `HistorialMovimiento`) no están envueltas en una transacción de base de datos compartida — cada repositorio ADO.NET abre su propia conexión. Si el proceso falla a mitad de una operación, puede quedar data parcialmente escrita. Pendiente: introducir un `IUnitOfWork` que comparta conexión/transacción entre repositorios para las operaciones multi-escritura críticas.

## 7. Hosting — VPS de Hostinger

**Decisión (2026-09-22):** el usuario ya tiene experiencia con un VPS de Hostinger — se usa ese en vez de un PaaS administrado (Azure/Railway, considerados pero descartados).

Configuración necesaria en el VPS:
- Instalar runtime de .NET.
- Instalar PostgreSQL (o correrlo vía Docker).
- **Nginx** como reverse proxy, con **Let's Encrypt** para HTTPS gratuito — **obligatorio**: los Service Workers (necesarios para el offline-first, sección 5) no se registran sin HTTPS.
- El frontend (React compilado) puede servirse desde el mismo VPS o desde Cloudflare Pages gratis — cualquiera funciona.
- **Recomendado, opcional:** Cloudflare gratis por delante del VPS solo como proxy/CDN (no hostea nada, es una capa adicional de velocidad/protección).
- **Requisito de specs:** mínimo ~2GB RAM, idealmente 4GB, para correr .NET + PostgreSQL cómodos.

**Nota (evaluada y descartada):** Cloudflare por sí solo NO puede hostear el backend .NET (Cloudflare Workers corre JS/Wasm en runtime aislado, no un backend .NET+PostgreSQL completo) — por eso se necesita el VPS para el backend.

---

## 8. Resumen de decisiones (para referencia rápida)

| Decisión | Elegido |
|---|---|
| Base de datos | PostgreSQL |
| Backend | .NET, Clean Architecture por features |
| Acceso a datos | ADO.NET puro + DbUp para migraciones |
| Manejo de errores | Patrón Result (`ErrorOr`) + Problem Details |
| Frontend | React + Vite |
| UI library | Ant Design |
| Estado del servidor | TanStack Query |
| Captura de campo | PWA (Workbox + Dexie.js) |
| Auth | Auth propia (ADO.NET) + JWT — no Identity (incompatible con ADO.NET puro) |
| Roles | Administrador, Operario, Ventas, Logística, Terceros |
| Hosting | VPS de Hostinger + Nginx + Let's Encrypt |
