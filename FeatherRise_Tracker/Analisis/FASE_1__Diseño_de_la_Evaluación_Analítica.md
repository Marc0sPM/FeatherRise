# FASE 1 — Diseño de la Evaluación Analítica

Este documento define la estrategia de telemetría implementada para *Feather Rise*. El objetivo es recopilar datos cuantitativos para validar o refutar de forma estadística las fricciones de diseño (brechas M, D y A) detectadas previamente durante el playtesting cualitativo.

---

## 1. Objetivos e hipótesis de evaluación

Se han seleccionado las hipótesis que resultaron refutadas (o parcialmente refutadas) en el análisis cualitativo de la Práctica 2, para medir su impacto real a través del comportamiento de los jugadores.

### Hipótesis 1 — Gestión de recursos y fricción de controles

- **Objetivo analítico:** Cuantificar la sobrecarga cognitiva y la fricción motriz generada por el sistema de recogida de plumas.
- **Hipótesis a validar:** El sistema actual genera un alto índice de intentos de recogida fallidos. Un intento se considera fallido y genera un bloqueo en el flujo de juego bien porque el jugador intenta recoger las plumas sin cumplir la condición necesaria (haber lanzado las 3) o bien porque pulsa la tecla de recogida (`W`) de forma errónea al intentar saltar.

### Hipótesis 4 — Dificultad y cuellos de botella generales

- **Objetivo analítico:** Analizar la distribución de muertes a lo largo de todo el juego para identificar cuellos de botella no previstos y evaluar la curva de dificultad global de los niveles.
- **Hipótesis a validar:** Existen picos de dificultad desbalanceados a lo largo de los niveles que provocan frustración, no limitándose únicamente a las zonas ya detectadas. Específicamente, se asume que áreas problemáticas como el puzle del nivel 2.2 concentrarán una tasa de muertes desproporcionadamente alta debido a fallos de diseño (falta de feedback visual), pero el análisis exploratorio permitirá descubrir otros posibles cuellos de botella en el resto del juego. Además, se correlacionará el tiempo de supervivencia por tramo con la cantidad de muertes para distinguir si la dificultad se debe a ensayo y error rápido (problema motriz/mecánico) o a atascos de comprensión (problema cognitivo).

### Hipótesis 5 — Exploración y recompensa (cofres)

- **Objetivo analítico:** Evaluar la tasa de interacción y el atractivo de las rutas secundarias.
- **Hipótesis a validar:** Los jugadores ignoran los cofres de las rutas secundarias porque no comprenden su utilidad ni asimilan qué ventaja les proporcionan.

### Hipótesis 7 — Comportamiento emergente en combate

- **Objetivo analítico:** Evaluar la eficiencia y el uso táctico del sistema de combate bimodal (ataque terrestre vs. aéreo) para identificar si los jugadores dominan las mecánicas o si recurren a estrategias de "spam".
- **Hipótesis a validar:** Debido al diseño de los encuentros, los jugadores subutilizan el ataque aéreo y abusan del ataque terrestre de forma descontrolada (*spam*). Esto se evidenciará en una desproporción en el uso de ataques terrestres frente a los aéreos y en una baja tasa de aciertos (*hit rate*) general.

---

## 2. Definición de métricas

| Código | Métrica | Hipótesis |
|:--:|:--|:--:|
| **M1.1** | Tasa de intentos de recogida fallidos (porcentaje de pulsaciones de `W` sin que se active la mecánica de recuperar las plumas). | H1 |
| **M4.1** | Distribución espacial de muertes (coordenadas X, Y, agrupables por nivel y por causa). | H4 |
| **M4.2** | Tiempo promedio por tramo (segundos transcurridos entre dos checkpoints consecutivos). | H4 |
| **M4.3** | Ratio de muertes por minuto en cada tramo (frecuencia de muertes asociada al checkpoint anterior). | H4 |
| **M5.1** | Tasa de apertura de cofres secundarios (proporción de sesiones que abrieron al menos un cofre en cada nivel). | H5 |
| **M7.1** | Porcentaje de uso por tipo de ataque (Ground vs. Aerial). | H7 |
| **M7.2** | Tasa de acierto de ataques o *Hit Rate* (porcentaje de ataques que impactan en un enemigo sobre el total de ataques realizados). | H7 |

---

## 3. Definición de eventos

Los siguientes eventos están implementados en `GameplayEvents.cs` (eventos de jugabilidad) y `SystemEvents.cs` (eventos de sesión). Todos heredan de `TrackerEvent`, que añade automáticamente `timestamp` (Unix UTC en segundos) y `session_id` (GUID) en el momento de la llamada a `Tracker.Instance.TrackEvent(...)`.

### Eventos de sistema (obligatorios por enunciado)

| Evento | Atributos | Cuándo se lanza |
|:--|:--|:--|
| `Session_Start` | — | Al inicializar el `Tracker` (un único evento por archivo). |
| `Session_End` | — | En `OnApplicationQuit`, antes del último *flush*. |
| `Level_Start` | `level_id` (int) | Al cargar una escena de nivel jugable (`GameManager.LoadCheckpointAndLevel`). |
| `Level_End` | `level_id` (int), `result` (string: "completed" / "quit") | Al cruzar la puerta final del nivel (`DoorComponent`) o al volver al menú principal (`UIManager.GoToMenu`). |

### Eventos de jugabilidad (asociados a las hipótesis)

| Evento | Atributos | Cuándo se lanza |
|:--|:--|:--|
| `Feather_Recall_Attempt` | `is_successful` (bool) | Cada vez que el jugador pulsa la tecla de recogida de plumas (`InputComponent`). `false` cuando ya está en proceso de recall o no se cumplen las condiciones; `true` cuando la mecánica se ejecuta. |
| `Player_Death` | `pos_x` (float), `pos_y` (float), `cause_of_death` (string) | Cuando la salud del jugador llega a 0. Las causas instrumentadas son `enemy_mele`, `enemy_range` (`GameManager`) y `void` (`VoidComponent`). |
| `Chest_Opened` | `chest_id` (string), `level_id` (int) | Al interactuar con éxito con un cofre (`ChestComponent`). `chest_id` es el nombre del prefab dropeado en minúsculas (`featheritem`, `sword`, etc.). |
| `Player_Attack` | `attack_type` (string: "ground" / "aerial"), `enemy_hit` (bool) | Cada vez que el jugador ejecuta un ataque (`PlayerCombat.Attack`). El tipo lo decide automáticamente la lógica del juego según `TouchingFloor`. |
| `Checkpoint_Reached` | `pos_x` (float), `pos_y` (float) | Cuando el jugador alcanza y activa un checkpoint (`GameManager.SetCheckpoint`) o al iniciar un nivel (checkpoint inicial). |

> **Nota de implementación:** En el diseño preliminar de la Práctica 2 se barajó incluir `level_id` también en `Player_Death` y `Checkpoint_Reached`. En la implementación final se descartó porque ese dato es deducible de forma externa: cada evento puede asociarse al `Level_Start` activo en su sesión (mismo `session_id` y `timestamp` posterior). Esto reduce el ancho de banda de las trazas sin perder información analítica.

---

## 4. Cálculo de las métricas a partir de los eventos

Todos los cálculos están implementados en `analyze_telemetry.py` (script reproducible y automático que se ejecuta sobre la carpeta de trazas; ver `INFORME_FASE_6_Entrega.md` para instrucciones).

| Métrica | Eventos usados | Fórmula / procedimiento |
|:--:|:--|:--|
| **M1.1** | `Feather_Recall_Attempt` | `nº eventos con is_successful == false / total de eventos`. Resultado expresado en porcentaje y desglosado por sesión para detectar si la fricción es transversal o atribuible a un único jugador. |
| **M4.1** | `Player_Death` | Se extraen los atributos `pos_x`, `pos_y` y `cause_of_death` de todos los eventos. El conjunto de coordenadas se renderiza como mapa de calor (KDE) por nivel mediante `seaborn`/`matplotlib`. Cuando hay menos de 2 muertes, el script degrada a un *scatter* anotado en lugar de KDE. |
| **M4.2** | `Checkpoint_Reached` | Para cada par de checkpoints consecutivos dentro de la misma sesión, se calcula `delta_s = ts(actual) − ts(anterior)`. Se reporta media, mediana, mínimo y máximo, agrupando por checkpoint destino redondeado a una decimal para identificar el tramo. |
| **M4.3** | `Player_Death` + `Checkpoint_Reached` | Se asocia cada muerte al `Checkpoint_Reached` inmediatamente anterior de la misma sesión (`pandas.merge_asof` con dirección *backward*). Se cruza el número de muertes por tramo con el tiempo total invertido en él (M4.2) y se calcula `muertes/min = muertes / tiempo_total_s × 60`. La interpretación cualitativa (atasco cognitivo vs. fricción motriz) se hace en el informe de análisis (Fase 4). |
| **M5.1** | `Chest_Opened` + `Level_Start` (o `level_id` de checkpoints) | Se cuentan los `session_id` únicos que generaron `Chest_Opened` en cada nivel y se divide entre el total de `session_id` que jugaron ese nivel. Se desglosa además por `chest_id` para distinguir cofres principales (p. ej. `featheritem` del tutorial) de los secundarios. |
| **M7.1** | `Player_Attack` | Se contabiliza el total de ataques. Se calcula el porcentaje de eventos con `attack_type == "ground"` y `attack_type == "aerial"`. |
| **M7.2** | `Player_Attack` | `aciertos / total = nº eventos con enemy_hit == true / total de eventos`. Se calcula el *hit rate* global y desglosado por `attack_type` para detectar si el problema de precisión es transversal o específico de uno de los dos tipos. |

## 5. Implementación del Sistema de Telemetría

Se ha desarrollado un sistema de telemetría modular para la recolección de eventos.

**[Enlace al repositorio del Sistema de Telemetría](../../Assets/Tracker/)**

**[Enlace Manual de uso del sistema de telemetría](../README_Instructions.md)**

---

## 6. Instrumentalización del Videojuego

Se han extendido las clases base del juego para instanciar el `Tracker` y disparar los eventos.

**[Enlace al repositorio del Juego Instrumentalizado](../../)**

**[Enlace de descarga a la Build Ejecutable](../)**

### 6.1. Clases Modificadas
* **`GameManager.cs`**: Modificado para inicializar el Tracker y gestionar los eventos de inicio/fin de nivel y checkpoints.
* **`PlayerCombat.cs`**: Instrumentado para capturar los eventos de ataque y distinguir entre `ground` y `aerial`.
* **`InputComponent.cs`**: Registra los intentos de recogida de plumas (`Feather_Recall_Attempt`).
* **`HealthComponent.cs` / `VoidComponent.cs`**: Capturan la muerte del jugador, enviando posición y causa.
* **`ChestComponent.cs`**: Dispara el evento al abrir con éxito un cofre.
