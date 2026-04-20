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

### 5.1. Partes opcionales

#### **`Serialización y persistencia en hebra independiente`**
A continuación se detalla cómo se ha integrado el uso de una hebra independiente para la persistencia. El script `Tracker.cs` sí utiliza multihilo, pero no de forma “pura” en todo momento. 
El sistema de telemetría utiliza un enfoque mixto (síncrono + asíncrono) para gestionar el guardado de eventos sin afectar al rendimiento del juego.

Modo asíncrono (multihilo): Por defecto, los datos se guardan en segundo plano usando `Task.Run`, lo que ejecuta la escritura en un hilo distinto al principal. Su objetivo es evitar bloqueos o caídas de rendimiento en el render del juego.

Modo síncrono (hilo principal): En momentos críticos (como al pausar o cerrar la aplicación), el guardado se realiza en el hilo principal. Su objetivo es garantizar que no se pierdan datos antes de que el proceso termine.
```pseudocode
// Tracker.cs - Flush(forceSynchronous)

if (forceSynchronous == true)
{
  // Modo síncrono (hilo principal)
  // Se usa en cierre o pausa del juego
  persistence.Save(data)
}
else
{
  // Modo asíncrono (multihilo)
  // Se usa durante el gameplay normal
  run_in_background_thread(() => {
    persistence.Save(data)
  })
}
```
#### **`Implementación de otro método de serialización`**
El sistema de telemetría permite exportar los eventos en **dos formatos distintos: `CSV` y `JSON`**. Esto hace posible adaptar la salida de datos según las necesidades del análisis posterior, la depuración o la integración con otras herramientas.
La elección del formato de serialización se puede realizar de dos formas:

### Configuración mediante archivo

Se puede definir desde el archivo de configuración `tracker.config.json`, modificando el valor del campo `serializer`.

```json
{
  "serializer": "JSON"
}
```
O bien:
```
{
  "serializer": "CSV"
}
---
```
También es posible configurar el tipo de serialización directamente desde el Editor de Unity.
Esto permite cambiar el formato sin necesidad de editar manualmente el archivo de configuración.

#### **`Envío de trazas a servidor web (Firebase)`**
El sistema permite enviar los eventos de telemetría a un servidor web utilizando **Firebase Realtime Database** mediante su API REST.
Para ello se han implementado dos componentes:

### Serialización (`FirebaseSerializer`)
Los eventos se convierten a formato **JSON** utilizando `JsonUtility`.  
Cada envío se construye como un **array independiente de eventos**, ya que Firebase no requiere mantener una estructura acumulativa entre envíos.
Ejemplo de salida:

```json
[
    { "event": "event_1", ... },
    { "event": "event_2", ... }
]
````

---

### Persistencia (`FirebasePersistence`)

El envío de datos se realiza mediante peticiones HTTP (`POST`) a la URL de Firebase:

```
https://featherrise-telemetry-p3-default-rtdb.europe-west1.firebasedatabase.app/
```

Para poder ver los datos usar la misma URL con .json al final y activar la casilla de `dar formato al texto`
```
https://featherrise-telemetry-p3-default-rtdb.europe-west1.firebasedatabase.app/.json
```

Características principales:

* Uso de `HttpClient` (thread-safe) para reutilizar conexiones.
* Envío en formato JSON (`application/json`, UTF-8).
* Ejecución desde un **hilo secundario** (integrado con el `Tracker`).
* Control de errores: si la petición falla, se lanza una excepción para reencolar los eventos.

---

## 6. Instrumentalización del Videojuego

Se han extendido las clases base del juego para instanciar el `Tracker` y disparar los eventos.

**[Enlace al repositorio del Juego Instrumentalizado](../../)**

**[Enlace de descarga a la Build Ejecutable](../)**

### 6.1. Clases Modificadas
A continuación se detalla cómo se ha integrado el sistema de telemetría (instrumentalización) dentro del código base del videojuego. Se han añadido llamadas al Singleton Tracker.Instance en los puntos clave de la lógica para capturar los eventos diseñados.

#### **`GameManager.cs`** 
Actúa como el núcleo principal para rastrear el flujo de la partida, los puntos de control y las muertes por combate. Se ha modificado en tres métodos distintos:
  * Evento `Level_Start` y `Checkpoint_Reached` inicial: En el método `Start()`, se registra el inicio del nivel capturando el ID de la escena actual, y se lanza el primer checkpoint en la posición inicial del jugador.
```pseudocode
  int levelId = SceneManager.GetActiveScene().buildIndex;
  Tracker.Instance.TrackEvent(new Level_Start(levelId)); 
  Tracker.Instance.TrackEvent(new Checkpoint_Reached(_respawnPoint.x, _respawnPoint.y));
```
* Evento `Checkpoint_Reached` en progreso: En el método `Checkpoint()`, cada vez que el jugador activa un punto de control, se guardan sus coordenadas.
```pseudocode
  public void Checkpoint(Vector2 respawnP)
  {
    _respawnPoint = respawnP;
    Tracker.Instance.TrackEvent(new Checkpoint_Reached(_respawnPoint.x, _respawnPoint.y));
  }
```
* Evento `Player_Death` (Combate): En el método `LoseSouls()`, cuando las almas llegan a 0, se evalúa qué tipo de enemigo asestó el golpe final (`SpinComponent` para melee o `ProyectileComponent` para rango) y se envía el evento de muerte correspondiente.
```pseudocode
  if(_souls <= 0)
  {
    if ((bool)enemy.GetComponent<SpinComponent>())
    {
        int levelId = SceneManager.GetActiveScene().buildIndex;
        Tracker.Instance.TrackEvent(new Player_Death(_player.transform.position.x, _player.transform.position.y, "enemy_mele")); // Nota: Corregido el pase del eje Y
    }
    else if ((bool)enemy.GetComponent<ProyectileComponent>())
    {
        int levelId = SceneManager.GetActiveScene().buildIndex;
        Tracker.Instance.TrackEvent(new Player_Death(_player.transform.position.x, _player.transform.position.y, "enemy_range"));
    }
  }
```

#### **`InputComponent.cs`**
Se ha instrumentalizado la detección de la recogida de plumas para validar la fricción del control (Métrica M1.1).
* Evento `Feather_Recall_Attempt`: En el método `Update()`, cuando el jugador pulsa la tecla "Feather Return", se evalúa si el jugador ya está llamando a las plumas o si aún no ha gastado todas (`GameManager.Instance.FeatherCant <= 0`). Se envía el evento con el flag booleano de éxito/fracaso.
```pseudocode
  if (Input.GetButtonDown("Feather Return"))
  {
    if(_isRecalling)
    {
        Tracker.Instance.TrackEvent(new Feather_Recall_Attempt(false));
        return; 
    }
    bool isSuccessful = GameManager.Instance.FeatherCant <= 0;
    Tracker.Instance.TrackEvent(new Feather_Recall_Attempt(isSuccessful));

    if(isSuccessful)
    {
        _isRecalling = true; 
    }
  }
```

#### **`PlayerCombat.cs`**
Encargado de monitorizar el estilo de combate del jugador para evaluar si se abusa del combate terrestre frente al aéreo (Métricas M7.1 y M7.2).
* Evento `Player_Attack`: Dentro del método `Attack()`, se ha implementado el rastreo bifurcado. Si el jugador está tocando el suelo, se envía un ataque tipo `Ground`; si está en el aire, tipo `Aerial`. Además, se calcula dinámicamente si el ataque impactó a algún enemigo (`_hitEnemies.Count() > 0`).
```pseudocode
  // PlayerCombat.cs - Attack() (Fragmento Ground)
  Collider2D[] _hitEnemies = Physics2D.OverlapCapsuleAll(_attackPoint.position, _attackSize, _direction, _angleAttack, _enemylayer | _rangeLayer);
  Tracker.Instance.TrackEvent(new Player_Attack(AttackType.Ground, _hitEnemies.Count() > 0));            

  // PlayerCombat.cs - Attack() (Fragmento Aerial)
  Collider2D[] _hitEnemisOnAir = Physics2D.OverlapCircleAll(_attackPoint.position, _radius, _enemylayer | _rangeLayer);
  Tracker.Instance.TrackEvent(new Player_Attack(AttackType.Aerial, _hitEnemisOnAir.Count() > 0));
```

#### **`ChestComponent.cs`**
Instrumentalizado para analizar la tasa de exploración y recolección secundaria (Métrica M5.1).
* Evento `Chest_Opened`: Dentro del método `Update()`, cuando el jugador interactúa con éxito con un cofre, se extrae el nombre del objeto instanciado (pasándolo a minúsculas para unificar) y se registra su apertura junto con el nivel actual.
```pseudocode
  // ChestComponent.cs - Update()
  if (Input.GetButtonDown("Interact"))
  {
    // [Código previo de instanciación y animación]
    string item_name = _content.name.ToLower();
    int levelId = SceneManager.GetActiveScene().buildIndex;
    Tracker.Instance.TrackEvent(new Chest_Opened(item_name,  levelId));
  }
```

#### **`VoidComponent.cs`**
Responsable de capturar las caídas al vacío, elemento clave para la detección de problemas de plataformeo en el diseño de niveles (Métrica M4.1 y M4.3).
* Evento `Player_Death` (Void): En `OnTriggerEnter2D`, si el objeto que colisiona es el jugador, se registra su muerte clasificando la causa como "void" y guardando su posición exacta al caer.
```pseudocode
  // VoidComponent.cs - OnTriggerEnter2D()
  if ((bool)collision.gameObject.GetComponent<InputComponent>())
  {
    // [Código de Respawn previo]
    Tracker.Instance.TrackEvent(new Player_Death(player.transform.position.x, player.transform.position.y, "void"));
  }
```

#### **`DoorComponent.cs`**
Gestiona la transición fluida y natural entre niveles, asumiendo una victoria en la escena actual.
* Evento `Level_End` (Completed): En el método `CambiarNivel()`, justo antes de cargar la siguiente escena, se registra que el nivel actual ha finalizado con un resultado de finalización exitosa.
```pseudocode
  // DoorComponent.cs - CambiarNivel()
  public void CambiarNivel(int _index)
  {
    int levelId = SceneManager.GetActiveScene().buildIndex;
    Tracker.Instance.TrackEvent(new Level_End(levelId, LevelResult.Completed)); 

    SceneManager.LoadScene(_index);
    GameManager.Instance.Featherslvl2();
  }
```
#### **`UIManager.cs`**
Capta los eventos de salida manual del juego (abandono).
* Evento `Level_End` (Quit): En el método `Quit()`, si el jugador decide salir del juego desde el menú de pausa hacia el menú principal, se registra que el nivel terminó por abandono (`Quit`).
```pseudocode
  // UIManager.cs - Quit()
  public void Quit()
  {
    int levelId = SceneManager.GetActiveScene().buildIndex;
    Tracker.Instance.TrackEvent(new Level_End(levelId, LevelResult.Quit));
    SceneManager.LoadScene(0);
  }
```
