# Feather Rise — Sistema de Telemetría

Este repositorio contiene la implementación y el análisis de telemetría para el videojuego **Feather Rise**. Este proyecto se ha realizado como parte de la **Práctica 3 — Sistema de Telemetría (Curso 2025/2026)**.

---

## Estructura de la Documentación

La entrega se divide en los siguientes bloques principales:

### 1. Diseño y Evaluación Analítica
Este documento detalla la base teórica y técnica del sistema implementado.
* **[Documento de Diseño de Evaluación](FeatherRise_Tracker/Analisis/FASE_1__Diseño_de_la_Evaluación_Analítica.md)**
    * **Objetivos e Hipótesis:** Preguntas de investigación planteadas para la evaluación.
    * **Métricas y Eventos:** Definición técnica de qué medimos y qué eventos lanzamos.
    * **Pipeline de Datos:** Descripción de cómo los eventos se transforman en métricas.
    * **Implementación de Telemetría:** Detalles de las partes opcionales y enlace al Repositorio del Sistema de Telemetría.
    * **Instrumentalización:** Explicación de las clases modificadas en el código del juego.
    * **Repositorio del Juego Instrumentalizado**
    * **[Descargar Build Instrumentalizada](FeatherRise_Tracker/)**

### 2. Análisis de Resultados y Conclusiones
Informe detallado tras procesar los datos recogidos en las sesiones de juego.
* **[Informe de Análisis de Resultados (Fase 4)](FeatherRise_Tracker/Analisis/INFORME_FASE_4_Analisis.md)**
    * Validación de hipótesis.
    * Detección de cuellos de botella (Heatmaps y tiempos).
    * Conclusiones de diseño y propuestas de mejora.

### 3. Datos y Trazas
Repositorio de los datos brutos utilizados para el análisis.
* **[Carpeta de Archivos de Datos/Trazas](FeatherRise_Tracker/Analisis/analisis/output)**
    * Incluye archivos `.json` / `.csv` generados por el sistema para garantizar la transparencia del análisis.

---

## Reproducibilidad del Análisis

Para volver a generar las gráficas y métricas presentadas en el informe, se adjunta el material necesario en la carpeta:
**[Material de Análisis y Código](FeatherRise_Tracker/Analisis/analisis)**
Será necesario leer el [README.md](FeatherRise_Tracker/Analisis/analisis/README.md) donde indican las instrucciones de forma más detallada, garantizado la reproducibilidad de la práctica, el README indica todo lo que es necesario para poder volver a generar las métricas: entorno de ejecución, versiones de lenguajes y librerías usadas y cómo instalarlas, dónde han de guardarse las trazas y cómo se ha de ejecutar el código entregado.

---
© 2025-2026 — Desarrollo de Videojuegos
