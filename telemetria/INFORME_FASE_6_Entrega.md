# INFORME FASE 6 — Entrega

**Práctica 3 — Sistema de telemetría · Curso 2025/2026**
**Juego:** *Feather Rise*

Este documento es el índice maestro de la entrega. Reúne en una sola vista todo lo que pide el apartado 6 del enunciado (objetivos, métricas, eventos, telemetría, instrumentalización, código de análisis, trazas y conclusiones), con los enlaces a los documentos y al material correspondiente.

---

## 1. Objetivos e hipótesis de la evaluación

Documento completo: **`FASE_1__Diseño_de_la_Evaluación_Analítica.md`** (sección 1).

Resumen de las 4 hipótesis evaluadas:

| Hipótesis | Foco | Métricas asociadas |
|:--:|:--|:--:|
| **H1** | Fricción del sistema de recall de plumas | M1.1 |
| **H4** | Cuellos de botella y curva de dificultad | M4.1, M4.2, M4.3 |
| **H5** | Exploración y atractivo de cofres secundarios | M5.1 |
| **H7** | Spam de ataque terrestre vs. uso del aéreo | M7.1, M7.2 |

---

## 2. Descripción de las métricas

Documento completo: **`FASE_1__Diseño_de_la_Evaluación_Analítica.md`** (sección 2).

| Código | Métrica |
|:--:|:--|
| M1.1 | Tasa de intentos de recogida de plumas fallidos |
| M4.1 | Distribución espacial de muertes |
| M4.2 | Tiempo promedio entre checkpoints |
| M4.3 | Ratio de muertes por minuto en cada tramo |
| M5.1 | Tasa de apertura de cofres secundarios |
| M7.1 | Reparto de tipo de ataque (Ground/Aerial) |
| M7.2 | Hit Rate global y por tipo de ataque |

---

## 3. Descripción de los eventos

Documento completo: **`FASE_1__Diseño_de_la_Evaluación_Analítica.md`** (sección 3).

Eventos implementados en el código del juego:

- **De sistema** (`SystemEvents.cs`): `Session_Start`, `Session_End`, `Level_Start`, `Level_End`.
- **De jugabilidad** (`GameplayEvents.cs`): `Feather_Recall_Attempt`, `Player_Death`, `Chest_Opened`, `Player_Attack`, `Checkpoint_Reached`.

Cada evento hereda de `TrackerEvent`, que añade automáticamente `timestamp` (Unix UTC en segundos) y `session_id` (GUID generado al inicio de la sesión).

---

## 4. Cómo se calcula cada métrica

Documento completo: **`FASE_1__Diseño_de_la_Evaluación_Analítica.md`** (sección 4).

Implementación reproducible: **`analyze_telemetry.py`**, función por función:

| Métrica | Función Python |
|:--:|:--|
| M1.1 | `metric_m11_feather_recall_failure()` |
| M4.1 | `metric_m41_death_distribution()` |
| M4.2 | `metric_m42_time_between_checkpoints()` |
| M4.3 | `metric_m43_deaths_per_minute()` |
| M5.1 | `metric_m51_chest_open_rate()` |
| M7.1 | `metric_m71_attack_type_breakdown()` |
| M7.2 | `metric_m72_hit_rate()` |

---

## 5. Telemetría — partes opcionales implementadas

El sistema de telemetría desarrollado va más allá de los requisitos mínimos del enunciado e incluye las siguientes partes opcionales (apartado 5 del enunciado):

### 5.1 Serialización y persistencia en hilo independiente

`Tracker.Flush()` lanza el `_persistence.Save(data)` mediante `Task.Run(() => ...)` cuando el modo es asíncrono (caso por defecto, durante el juego). Sólo bloquea cuando `forceSynchronous == true`, que se activa únicamente en `OnApplicationQuit` para garantizar que el último volcado llega a disco antes de que Windows mate los hilos secundarios. Ver `Tracker.cs` líneas 105-117.

### 5.2 Segundo formato de serialización (CSV)

Implementado en `CSVSerializer.cs`, configurable desde el editor de Unity mediante el enum `S_type` del `TrackerInitializer`. El CSV contiene las columnas `timestamp`, `session_id`, `event_type` y `event_data` (esta última con el JSON completo del evento, lo que conserva todos los atributos sin perder información). El analizador de la Fase 4 lee ambos formatos indistintamente.

### 5.3 Persistencia alternativa: envío a Firebase

Implementado en `FirebasePersistence.cs`. Usa `HttpClient` estático (thread-safe) y publica vía REST en `{databaseUrl}/telemetry/{sessionId}.json`. Cuando se selecciona persistencia Firebase, el sistema fuerza automáticamente el uso de `FirebaseSerializer.cs` (formato JSON adaptado a las restricciones del REST API de Firebase Realtime Database). Configurable desde el inspector del `TrackerInitializer` mediante el enum `P_type` y el campo `firebaseDatabaseUrl`.

### 5.4 Configuración del sistema desde el editor de Unity

Toda la configuración del tracker (tipo de serializador, tipo de persistencia, URL del servidor Firebase, intervalo de auto-flush) está expuesta en el inspector de Unity vía `[SerializeField]` y `[Header]` en `TrackerInitializer.cs` y `Tracker.cs`. No requiere recompilación para cambiar el modo de salida.

### 5.5 Auto-flush configurable

El campo `autoFlushInterval` (default 30 s) en `Tracker.cs` permite ajustar cada cuánto se vuelca la cola de eventos a disco/red. El volcado se ejecuta en una corutina (`AutoFlushCoroutine`) que no bloquea el hilo principal del juego.

### 5.6 Tolerancia a fallos del sistema de telemetría

`Tracker.Flush()` envuelve el `Save` en try-catch y, si falla, devuelve los eventos a la cola con `ReturnEventsToQueue()` para reintentar en el siguiente *flush*. El juego sigue funcionando aunque el tracker falle: las llamadas a `Tracker.Instance.TrackEvent()` desde el código del juego no levantan excepciones porque sólo encolan, y la encolación está aislada del proceso de serialización.

### 5.7 Enlace al repositorio

> [Pegar aquí el enlace al repositorio de GitHub/GitLab donde está el sistema de telemetría]

---

## 6. Instrumentalización del videojuego

Listado de clases del juego que han sido modificadas para insertar las llamadas a `Tracker.Instance.TrackEvent(...)`:

| Clase | Eventos emitidos | Punto de inserción |
|:--|:--|:--|
| `Tracker.cs` | `Session_Start`, `Session_End` | Init / OnApplicationQuit |
| `GameManager.cs` | `Level_Start`, `Checkpoint_Reached`, `Player_Death` (causas `enemy_mele`, `enemy_range`) | LoadCheckpointAndLevel, SetCheckpoint, EvalueG |
| `DoorComponent.cs` | `Level_End` (result = `completed`) | OnTriggerEnter2D al cruzar la puerta |
| `UIManager.cs` | `Level_End` (result = `quit`) | GoToMenu (botón salir al menú) |
| `VoidComponent.cs` | `Player_Death` (causa `void`) | OnTriggerEnter2D al caer al vacío |
| `InputComponent.cs` | `Feather_Recall_Attempt` (true/false) | Update, al detectar el botón "Feather Return" |
| `PlayerCombat.cs` | `Player_Attack` (Ground/Aerial + enemy_hit) | Attack(), tras el OverlapCapsuleAll/OverlapCircleAll |
| `ChestComponent.cs` | `Chest_Opened` | Update, al detectar input de interacción |

### 6.1 Garantía de que el juego funciona si el tracker falla

Todas las llamadas a `Tracker.Instance.TrackEvent(...)` van a una `ConcurrentQueue` que no levanta excepciones por escritura. El `Tracker` es un singleton; si por algún motivo el GameObject del tracker no se ha inicializado, la llamada estática fallaría con `NullReferenceException`, pero esto está mitigado porque:

1. El `TrackerInitializer` se carga antes que las escenas jugables.
2. El `Tracker` es `DontDestroyOnLoad`, así que persiste entre escenas.
3. Cualquier excepción en serialización/persistencia se captura dentro del propio `Tracker.Flush()` y no propaga al hilo principal del juego.

### 6.2 Enlaces

- **Repositorio del juego instrumentalizado:** [pegar enlace]
- **Build jugable con telemetría activa:** [pegar enlace]

---

## 7. Material para el cálculo automático de las métricas

### 7.1 Estructura de archivos entregados

```
entrega/
├── FASE_1__Diseño_de_la_Evaluación_Analítica.md   # objetivos, métricas, eventos
├── INFORME_FASE_4_Analisis.md                      # informe de resultados
├── INFORME_FASE_6_Entrega.md                       # este documento
├── analisis/
│   ├── analyze_telemetry.py                        # script reproducible
│   ├── requirements.txt                            # dependencias Python
│   ├── README.md                                   # instrucciones de uso
│   ├── data/                                       # trazas de telemetría originales
│   │   ├── telemetry_5bdc5c40...json
│   │   ├── telemetry_92ea5e60...json
│   │   ├── telemetry_a400e812...json
│   │   └── telemetry_cfc2d9dc...csv
│   └── output/                                     # resultados generados (auto)
│       ├── metrics_summary.json
│       ├── metrics_summary.md
│       ├── events_normalized.csv
│       └── figures/
│           ├── M1_1_feather_recall.png
│           ├── M4_1_death_distribution.png
│           ├── M4_2_time_per_tramo.png
│           ├── M5_1_chest_opening.png
│           └── M7_attack_analysis.png
```

### 7.2 Entorno de ejecución

| Componente | Versión usada | Versión mínima |
|:--|:--|:--|
| Python | 3.12 | 3.10 |
| pandas | 3.0.1 | 2.0 |
| matplotlib | 3.10.8 | 3.7 |
| numpy | 2.4.3 | 1.24 |
| seaborn | 0.13.2 | 0.12 |

### 7.3 Instalación

```bash
# Crear y activar entorno virtual (recomendado)
python3 -m venv venv
source venv/bin/activate          # Linux / macOS
# venv\Scripts\activate           # Windows

# Instalar dependencias
pip install -r analisis/requirements.txt
```

Contenido de `requirements.txt`:

```
pandas>=2.0
matplotlib>=3.7
numpy>=1.24
seaborn>=0.12
```

### 7.4 Ejecución

```bash
cd analisis
python analyze_telemetry.py --data data --output output
```

Argumentos opcionales:

- `--data DIR` (default `data`): carpeta con archivos `.json` y `.csv` de telemetría.
- `--output DIR` (default `output`): carpeta donde se escriben los resultados.

El script es completamente automático: lee todos los archivos, aplica la limpieza, calcula las 7 métricas, genera las gráficas y vuelca los resultados. **No requiere ninguna intervención del usuario más allá de lanzar el comando.**

### 7.5 Dónde colocar nuevas trazas para reproducir

Cualquier archivo `.json` o `.csv` que se deposite en la carpeta `analisis/data/` será procesado automáticamente en la siguiente ejecución del script. No es necesario tocar el código.

---

## 8. Documento de análisis y conclusiones

Documento completo: **`INFORME_FASE_4_Analisis.md`**.

Resumen del veredicto sobre cada hipótesis:

| Hipótesis | Veredicto | Evidencia |
|:--:|:--|:--|
| **H1** | ✅ Confirmada | 40,9 % de intentos de recall fallidos (9/22), consistente entre sesiones. |
| **H4** | ⚠️ No concluyente por insuficiencia de datos | Sólo 1 muerte registrada en todo el corpus (causa `void`); ninguna sesión llegó al nivel 2.2. |
| **H5** | ❌ Refutada parcialmente | 75 % de las sesiones abrieron cofres del nivel 1; falta validar el caso clave del nivel 2.2. |
| **H7** | ✅ Confirmada con fuerza | 93,75 % de ataques `ground` vs 6,25 % `aerial`; *hit rate* global del 31,25 %. |

Como hallazgo colateral, el análisis ha permitido detectar **2 bugs en el propio sistema de telemetría** (descritos en la sección 1 del informe de Fase 4) que conviene arreglar antes de la siguiente tanda de recogida.
