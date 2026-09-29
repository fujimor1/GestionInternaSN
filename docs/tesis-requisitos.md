# Notas y requisitos de la tesis (Sierra Nevada)

**Fecha de creación:** 2026-09-21
**Propósito de este documento:** llevar aparte todo lo relacionado a los requisitos académicos/formales de la tesis (formato, metodología, fechas, artefactos exigidos), separado de la investigación técnica del dominio (que vive en `docs/investigacion-parametros-produccion.md`). La idea es no mezclar "qué necesita el sistema para funcionar bien" con "qué necesita la tesis para aprobarse".

---

## 1. Naturaleza del proyecto

Este es un **proyecto real**, no una maqueta académica — sirve simultáneamente para:
- El **cliente real**: Sierra Nevada (piscigranja de truchas), que necesita el sistema migrado (Django → C#/React) y funcionando en producción.
- La **tesis del usuario**, que se sustenta ante un jurado universitario.

**Implicación de alcance:** las fases 1 y 2 del motor de producción (proyección de campaña + calibración estadística — ver `docs/investigacion-parametros-produccion.md` sección 8.2) tienen que quedar **realmente desplegadas y en uso**, no solo diseñadas en papel. La fase 3 (Machine Learning) puede quedar como trabajo futuro documentado o prueba de concepto, pero 1 y 2 no.

## 2. Fecha límite

El usuario termina la universidad a **fines de 2026** — esa es la fecha límite implícita para tener algo sustentable.

## 3. Información pendiente de conseguir (bloquea cómo se documenta el desarrollo)

- [ ] **Formato oficial de tesis de la universidad**: estructura de capítulos exigida (Introducción / Marco Teórico / Metodología / Desarrollo / Resultados / Conclusiones, o el esquema que usen), normas de citado (APA/IEEE/ISO), plantilla oficial (Word/LaTeX) si existe.
- [ ] **1-2 tesis ya aprobadas de la facultad** (idealmente de sistemas/software), como referencia de profundidad esperada y forma de documentar el desarrollo técnico.
- [ ] **Metodología de desarrollo de software exigida** (si la hay): RUP, Scrum, XP, ICONIX, TSDLC, etc. Esto determina qué artefactos son obligatorios (ver punto 4).
- [ ] **Título de la tesis y objetivos** (general y específicos), si ya están planteados — para que la documentación apunte directo a sustentarlos.
- [ ] **Fechas clave**: sustentación, entregas parciales que revisa el asesor, y qué espera cada una.
- [ ] **Rúbrica de evaluación del jurado**, si la tienen.

## 4. Preguntas abiertas específicas de la tesis (no técnicas del sistema)

1. **¿Casos de uso o historias de usuario?** No depende tanto del título como de la metodología de desarrollo elegida — RUP/ICONIX normalmente exigen casos de uso como artefacto obligatorio; Scrum/XP suelen reemplazarlos por historias de usuario (aunque algunos asesores piden ambos). **Pendiente de confirmar con el asesor** o la guía de tesis de la universidad, una vez se sepa la metodología.

## 5. Relación con la documentación técnica existente

- `docs/investigacion-parametros-produccion.md` — investigación técnica del dominio (contraste Excel/FONDEPES/Django, estándares de calidad, arquitectura de 3 fases). Es material crudo aprovechable para el capítulo de **Marco Teórico / Estado del Arte**, pero está escrito como bitácora de trabajo, no en formato de tesis — se reestructurará una vez se tenga el formato oficial (punto 3).
- Este documento (`tesis-requisitos.md`) es solo para lo académico/administrativo — no debe mezclarse con hallazgos de truchicultura ni decisiones de arquitectura del sistema.
