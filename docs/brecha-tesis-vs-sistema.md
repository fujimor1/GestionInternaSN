# Brecha entre lo que dice la tesis y lo que existe en el sistema

**Fecha del análisis:** 2026-09-26 (revisión de `docs/tesis_untels_capitulos_1_y_2 (1).pdf`).

**Por qué existe este documento:** el sistema que se ha venido construyendo en este repositorio (motor de producción — lotes, campañas, muestreos, curva de crecimiento, calibración K/FCA/mortalidad) es la base zootécnica del proyecto, pero **no es, por sí solo, lo que la tesis promete resolver.** El título de la tesis es *"Desarrollo de un sistema web basado en Machine Learning para mejorar la gestión del inventario de alimento en una empresa acuícola, Huaral, 2026"* — el problema central es la **gestión del inventario de alimento balanceado** (Kardex, punto de reorden, cantidad económica de pedido), no la producción biométrica. Este documento deja registrado, punto por punto, qué tan lejos está el sistema actual de lo que el Capítulo 1 y el Capítulo 2 de la tesis describen, para que sirva de lista de pendientes antes de la sustentación.

---

## 1. El centro real de la tesis: gestión de inventario de alimento

El diagnóstico del Capítulo 1 (sección 1.1) identifica cinco problemas concretos en la empresa. El primero y más importante es este:

> *"Gestión manual, dispersa y no estandarizada del inventario de alimento: el registro de las recepciones de sacos de alimento por proveedor, fecha de caducidad, número de lote de fábrica y calibre de pellet (micraje), así como los egresos diarios destinados a cada estanque, se efectúa exclusivamente en libretas de papel..."*

Y el objetivo específico #2 de la tesis (sección 1.3.2) es:

> *"Evaluar la influencia de la capacidad predictiva del modelo de Machine Learning en la planificación del reorden de alimento..."*

**Esto requiere un módulo de Kardex de alimento que hoy no existe en el sistema.** Lo que sí existe es `RegistroAlimentacionReal` — que registra cuántos kilos se le dieron de comer a un lote en una fecha (el *consumo*, ya proyectado hacia el pez). Eso es distinto y no reemplaza a un Kardex de *stock comprado*: no hay tabla de compras/recepciones por proveedor, no hay fecha de caducidad, no hay calibre de pellet, no hay saldo valorizado, y no hay las tres fórmulas que la tesis cita explícitamente (sección 2.2.6, ecuaciones 2.12–2.14):

- **Punto de Reorden (ROP)** = `(demanda promedio diaria × lead time) + Stock de Seguridad`
- **Stock de Seguridad (SS)** = `Z × desviación estándar de la demanda × √(lead time)`
- **Cantidad Económica de Pedido (EOQ)** = `√(2 × demanda anual × costo por pedido / costo de almacenamiento)`

Sin este módulo, el objetivo específico #2 completo de la tesis no tiene con qué sustentarse en una demo real.

---

## 2. Otras discrepancias encontradas (arquitectura y alcance)

### 🔴 Críticas — riesgo real en la sustentación

- **Backend: la tesis describe Entity Framework Core 8 como ORM** (sección 2.2.1: *"Implementa el acceso a datos mediante el ORM Entity Framework Core 8... Fluent API... migraciones versionadas"*). El sistema real usa **ADO.NET + DbUp**, sin ORM — decisión tomada explícitamente al inicio del proyecto (ver `docs/arquitectura-tecnica.md`). Si un jurado pide ver el `DbContext` o las migraciones de EF, no existen.
- **MediatR + CQRS + FluentValidation** — la tesis describe Comandos/Consultas vía MediatR con el patrón CQRS y validación con FluentValidation en la capa de Aplicación. El código real usa clases de servicio directas (`RegistrarMuestreoService`, `CrearCampañaConLotesService`, etc.) con `ErrorOr`, sin Mediator ni FluentValidation.
- **Ubicación de la empresa no coincide.** El Capítulo 1 describe la granja en **Santa Catalina, Huaral** (Lima, costa) — toda la investigación previa y el sistema construido se basaron en una granja a **~4,500 msnm** (altiplano). Esto afecta hasta las referencias de oxígeno disuelto y temperatura citadas en el marco teórico. Pendiente de aclarar si es la misma empresa con otra sede, o si el caso de estudio de la tesis es distinto al cliente real del sistema.

### 🟡 Menores — hay que alinear, pero no son estructurales

- **React 18 mencionado → el proyecto real usa React 19.**
- **TailwindCSS mencionado junto a Ant Design → Tailwind no está instalado** en el frontend (verificado en `package.json`); solo se usa Ant Design con estilos inline.
- **Roles:** la tesis dice *"Administrador, Jefe de Producción, Operario"* → el sistema real tiene 5 roles: `Administrador, Operario, Ventas, Logistica, Terceros`. No existe el rol "Jefe de Producción".
- **SUNAT / facturación electrónica** mencionada en la capa de Infraestructura → no implementado, no hay rastro en el código.
- **`AlimentoKardex`** aparece listada como entidad de dominio en la tesis → no existe (consecuencia directa del punto 1 de este documento).
- **`MotorBayesianoAdaptativo`** se describe como si ya estuviera implementado en la capa `Domain` → en realidad solo está **documentado** (`docs/ml-fase3-formulas.md`), no hay código C# todavía. Revisar el tiempo verbal en la tesis para no afirmar algo que aún no está construido.

### ✅ Lo que sí coincide bien

- Clean Architecture de 4 capas (Domain/Application/Infrastructure/Api).
- Entidades `Lote`, `Campaña`, `UnidadProduccion`, `Muestreo`, `RegistroCondiciones` — existen tal cual las describe la tesis.
- ASP.NET Core 8, PostgreSQL 16.
- PWA Offline-First con Dexie.js — **sí está implementado** (`frontend/src/offline/db.ts`).
- Las fórmulas TGC (Grados-Día) y de actualización bayesiana conjugada (ecuaciones 2.1–2.10 del Capítulo 2) — **coinciden exactamente** con lo documentado en `docs/ml-fase3-formulas.md`. Esta parte del marco teórico está perfectamente alineada con el diseño real.

---

## 3. Siguiente paso recomendado

Antes de seguir puliendo el módulo de producción, conviene decidir con el usuario:

1. **Construir el módulo de Kardex/ROP/EOQ** — es lo más grande y lo que la tesis realmente necesita sustentar (objetivo específico #2 completo depende de esto).
2. **Ajustar el texto de la tesis** para que describa con precisión lo que existe hoy (ADO.NET en vez de EF Core, sin MediatR, los 5 roles reales, aclarar Huaral vs. altiplano).
3. Ambas cosas, por partes.

Ninguna decisión se ha tomado todavía sobre esto — queda pendiente de confirmar con el usuario.
