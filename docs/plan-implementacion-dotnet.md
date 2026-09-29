# Plan de implementación — migración a .NET + React

**Fecha:** 2026-09-22
**Contexto:** este documento define el orden de trabajo para construir el sistema nuevo en **.NET (backend) + React (frontend)**, reemplazando Django por completo — no hay implementación intermedia en Django, ni siquiera para la captura de datos nueva (ver corrección en `docs/diseno-modulo-produccion.md`).

**Verificado 2026-09-22:** `db.sqlite3` (Django actual) solo tiene datos de prueba — 5 lotes, 0 ventas registradas, pocas filas en cada tabla. **No hay historial real que migrar.** El sistema nuevo arranca con esquema limpio, diseñado según `docs/diseno-modulo-produccion.md`, no portado 1:1 de los modelos de Django.

---

## 1. Decisiones técnicas pendientes (bloquean el arranque)

Antes de crear el proyecto, necesito que definas esto — son decisiones tuyas, no algo que pueda asumir:

1. **Motor de base de datos.** Con .NET + Entity Framework Core, las opciones típicas son SQL Server o PostgreSQL. Mi recomendación sería PostgreSQL (gratis, multiplataforma, buen soporte en EF Core) salvo que ya tengas licencia/infraestructura de SQL Server disponible. ¿Cuál prefieres?
2. ~~Hosting/despliegue — conectividad.~~ **Resuelto (2026-09-22):** el cliente confirmó que la conectividad en la granja es variada — hay zonas sin señal. **Decisión:** las pantallas de captura de campo (`Muestreo`, `RegistroAlimentacionReal`, `RegistroMortalidad`, `RegistroCondiciones`) se construyen como **PWA** (Progressive Web App) — sigue siendo la misma app web en React, con Service Worker + IndexedDB agregados para que funcione sin señal y sincronice automáticamente al recuperar conexión. No es una app móvil nativa ni un proyecto aparte. El resto del sistema (reportes, configuración, ventas) es una web normal sin esta capa, ya que se usa desde una oficina con conexión.
3. **Autenticación/roles.** Django tenía `CustomUser` (con DNI, pregunta de seguridad) + grupos/permisos estándar de Django. En .NET esto se traduce a ASP.NET Core Identity + JWT para la API, con roles. ¿Los roles que necesita el sistema real ya están definidos (ej. Administrador, Operador de campo, Ventas), o hay que definirlos desde cero?
4. **Detalles del frontend.** React confirmado, pero falta: ¿Vite o Next.js? ¿Librería de UI (Material UI, Ant Design, Tailwind + componentes propios)? ¿Manejo de estado (Redux, Zustand, Context)? No bloquea tanto como los puntos 1-3, pero conviene decidirlo antes de crear el proyecto.
5. **Metodología de desarrollo para la tesis** (pendiente en `docs/tesis-requisitos.md`) — no bloquea empezar a programar, pero si tu asesor exige una metodología específica (RUP, Scrum, etc.), conviene saberlo antes de avanzar mucho para no tener que reestructurar la documentación del desarrollo después.

## 2. Orden de implementación propuesto

**Fase 0 — Scaffolding**
- Crear la solución .NET (Web API), estructura en capas.
- Configurar EF Core + base de datos elegida (punto 1).
- Crear proyecto React (punto 4).
- Configurar autenticación (punto 3).

**Fase 1 — Núcleo de producción (entidades base)**
- Migrar (rediseñadas, no copiadas) las entidades base: `Jaula`, `Artesa`, `Campaña` (nueva), `Lote` (con `lote_padre`), `Bastidor`.
- CRUD + endpoints API.
- Frontend: pantallas de gestión de campañas/lotes/jaulas.

**Fase 2 — Captura real (habilita todo el motor de calibración)**
- `Muestreo`, `RegistroAlimentacionReal`, extensión de `HistorialMovimiento` (`CAMBIO_ETAPA`), `RegistroCondiciones`, `RegistroMortalidad`.
- Frontend: formularios de campo — deben ser simples y rápidos de usar por alguien físicamente en la jaula, idealmente pensados para el escenario de conectividad del punto 2.

**Fase 3 — Motor de proyección** (fase 1 del negocio, ver `docs/investigacion-parametros-produccion.md` sección 8.2)
- `TablaReferenciaVersion`/`TablaReferenciaValor` cargadas con las tablas semilla FONDEPES + decisiones ya validadas con el cliente (sección 6 del doc de investigación).
- Cálculo día a día de biomasa/ración/FCA proyectado (equivalente al Excel actual).
- Alertas de densidad (sección 8.1 del doc de investigación).

**Fase 4 — Motor de calibración** (fase 2 del negocio)
- Cálculo de K real, FCA real, mortalidad real, duración real por etapa (a partir de los datos de la Fase 2 de este plan).
- Comparación proyectado vs. real, generación de nuevas versiones de `TablaReferencia`.

**Fase 5 — Resto de módulos**
- Comercialización (0 datos reales hoy, esquema simple de ventas) y logística — se migran después de producción, ya definido desde el inicio del proyecto como prioridad.

**Fase 6 (futuro, fuera de alcance de la tesis por ahora)**
- Motor adaptativo con Machine Learning — ver `docs/investigacion-parametros-produccion.md` sección 8.2.

## 3. Qué falta para poder arrancar

- [ ] Respuestas a los 5 puntos de la sección 1 (bloquean crear el proyecto).
- [ ] Confirmar con el cliente el volumen real de cada jaula/artesa (para densidad real, ya identificado como pendiente en `docs/diseno-modulo-produccion.md`).
- [ ] Metodología de tesis (no bloquea el código, sí la documentación formal del desarrollo).
