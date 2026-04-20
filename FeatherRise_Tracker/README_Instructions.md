# Manual de uso del sistema de telemetría

Sistema de telemetría para *Feather Rise*. Recoge eventos durante la sesión de juego y los persiste a disco local o a Firebase Realtime Database.

---

## Índice

1. [Qué hace el tracker y qué captura](#1-qué-hace-el-tracker-y-qué-captura)
2. [Puesta en marcha rápida](#2-puesta-en-marcha-rápida)
3. [Dónde se guardan las trazas](#3-dónde-se-guardan-las-trazas)
4. [Fichero de configuración](#4-fichero-de-configuración)
5. [Cómo instrumentar un evento nuevo en el juego](#5-cómo-instrumentar-un-evento-nuevo-en-el-juego)
6. [Casos de uso típicos](#6-casos-de-uso-típicos)
7. [Qué hacer con las trazas](#7-qué-hacer-con-las-trazas)
8. [Resolución de problemas](#8-resolución-de-problemas)

---

## 1. Qué hace el tracker y qué captura

El tracker se inicializa automáticamente al arrancar el juego y registra eventos en una cola interna. Cada 30 segundos (configurable) el contenido de la cola se vuelca a disco o se envía a Firebase.

**Eventos capturados por defecto:**

| Evento | Se dispara cuando… |
|:--|:--|
| `Session_Start` / `Session_End` | Arranca / se cierra el juego |
| `Level_Start` / `Level_End` | Se carga / se cruza la puerta final de un nivel |
| `Checkpoint_Reached` | El jugador activa un checkpoint |
| `Player_Death` | La salud del jugador llega a 0 (causa: `void`, `enemy_mele`, `enemy_range`) |
| `Feather_Recall_Attempt` | El jugador pulsa la tecla de recall de plumas |
| `Player_Attack` | El jugador ataca con la espada (tipo: `ground` / `aerial`) |
| `Chest_Opened` | El jugador interactúa con un cofre |

Todos los eventos llevan automáticamente `timestamp` (Unix UTC) y `session_id` (GUID único por sesión).

---

## 2. Puesta en marcha rápida

### En el editor de Unity

1. El prefab con los componentes `Tracker` y `TrackerInitializer` ya está en la escena principal del juego. No se requiere configuración adicional para empezar a recoger datos.
2. Le das al Play. En la consola verás un log del tipo:
   ```
   [TrackerInitializer] Trazas locales en: C:\...\Feather Rise\Telemetry
   [TRACKER] Inicializado. Sesion ID: 27c6ca73-9d40-4f9e-bde3-c7223bbf6dc0 · Flush cada 30s
   ```
3. Juegas normalmente. Cada 30 segundos se vuelca un lote de eventos al disco.
4. Cuando cierras el juego, se escribe el `Session_End`, se hace un último flush y se cierra el archivo.

### En una build

La build ya incluye todo. Funciona igual que en el editor salvo que la carpeta `Telemetry/` aparece junto al ejecutable (si se usa el modo por defecto `NextToExe`).

---

## 3. Dónde se guardan las trazas

Depende del modo configurado. Hay 3 opciones:

| Modo | Ruta resultante | Recomendado para |
|:--|:--|:--|
| **`NextToExe`** (default) | `{carpeta_del_exe}/Telemetry/` | Playtesting local, compartir fácilmente |
| **`PersistentData`** | `%APPDATA%\LocalLow\{empresa}\{producto}\Telemetry\` (Windows) | Builds publicadas en Steam u otras tiendas |
| **`Custom`** | Cualquier ruta absoluta que definas | Entornos especiales (red compartida, NAS) |

En modo editor, `NextToExe` resuelve a la **raíz del proyecto** (la carpeta que contiene `Assets/`), así que si estás en el editor las trazas aparecen ahí.

### Nombres de archivo

Formato: `telemetry_{session_id}_{yyyymmdd_hhmmss}.{json|csv}`

Ejemplo: `telemetry_27c6ca73-9d40-4f9e-bde3-c7223bbf6dc0_20260419_142503.json`

Si la rotación está activa (campo `fileRotationMaxMb > 0`), los archivos adicionales llevan sufijo `_part002`, `_part003`, etc.

---

## 4. Fichero de configuración

El tracker puede leer su configuración de un fichero JSON externo al arrancar. Esto te permite **cambiar la configuración en una build sin recompilar**.

### Dónde colocarlo

El tracker busca `tracker.config.json` en este orden:

1. **Junto al ejecutable** (o en la raíz del proyecto si estás en editor). Esta es la ubicación recomendada.
2. **En `persistentDataPath`** (fallback para entornos donde el usuario no tiene permisos junto al ejecutable).
3. Si no encuentra el fichero, usa los valores del Inspector del `TrackerInitializer`.

### Cómo generar la plantilla

Dos formas:

- Copiar el fichero `tracker.config.json` incluido en este repositorio junto al ejecutable.
- O bien, en el editor: click derecho sobre el componente `TrackerInitializer` → **Generate config file next to exe**. Esto vuelca los valores actuales del Inspector al fichero.

### Campos disponibles

| Campo | Tipo | Default | Explicación |
|:--|:--|:--|:--|
| `enabled` | bool | `true` | Master on/off. Con `false` el tracker no se inicializa y el juego corre sin telemetría. |
| `verboseLogging` | bool | `false` | Log en consola de cada evento encolado. Solo para depurar, muy ruidoso. |
| `serializer` | string | `"JSON"` | `"JSON"` o `"CSV"`. Formato del archivo de salida. |
| `persistence` | string | `"LocalFile"` | `"LocalFile"` o `"Firebase"`. Destino de las trazas. |
| `localFileOutputMode` | string | `"NextToExe"` | `"NextToExe"`, `"PersistentData"` o `"Custom"`. Ver tabla del apartado 3. |
| `customOutputDir` | string | `""` | Ruta absoluta usada solo si `localFileOutputMode == "Custom"`. |
| `fileRotationMaxMb` | float | `0.0` | Tamaño máximo del archivo en MB antes de rotar (`0` = sin rotación). |
| `firebaseDatabaseUrl` | string | `""` | URL de la Realtime DB de Firebase (solo si `persistence == "Firebase"`). |
| `autoFlushIntervalSeconds` | float | `30.0` | Segundos entre flushes automáticos de la cola a disco/red. |
| `fileBufferSizeBytes` | int | `4096` | Buffer del `FileStream`. Más grande = menos I/O pero más pérdida ante crash. |
| `disabledEventTypes` | string[] | `[]` | Lista de nombres de clase de eventos a ignorar (p. ej. `["Player_Attack"]`). |

---

## 5. Cómo instrumentar un evento nuevo en el juego

Tres pasos. Supongamos que queremos registrar cuándo el jugador realiza un doble salto.

### Paso 1 — Declarar el evento

En `GameplayEvents.cs` (o un fichero nuevo) añadir:

```csharp
[System.Serializable]
public class Player_DoubleJump : TrackerEvent
{
    public float pos_x;
    public float pos_y;

    public Player_DoubleJump(float x, float y) : base()
    {
        this.pos_x = x;
        this.pos_y = y;
    }
}
```

El constructor de `TrackerEvent` rellena automáticamente `event_type` con el nombre de la clase. No hay que tocar `timestamp` ni `session_id`, los pone el tracker.

### Paso 2 — Llamar al tracker desde el código del juego

Donde se ejecute la lógica del doble salto, típicamente en `MovementComponent.cs`:

```csharp
if (puedeHacerDobleSalto) {
    // ... lógica del salto ...
    Tracker.Instance.TrackEvent(new Player_DoubleJump(
        transform.position.x,
        transform.position.y
    ));
}
```

### Paso 3 — (Opcional) Permitir desactivarlo por config

Si añades `"Player_DoubleJump"` a `disabledEventTypes` en el fichero de configuración, el tracker descartará ese evento sin encolarlo. Útil para probar builds sin registrar eventos concretos.

---

## 6. Casos de uso típicos

### Playtesting local con un compañero

1. Build con `localFileOutputMode: "NextToExe"`.
2. El compañero juega. Cuando termine, te pasa la carpeta `Telemetry/` que ha aparecido junto al `.exe`.
3. Pegas los archivos `.json` en tu carpeta `analisis/data/` y ejecutas `python analyze_telemetry.py`.

### Playtesting con varios usuarios en paralelo (red compartida)

1. Crear carpeta en un servidor/NAS accesible para todos: `\\server\telemetry\feather_rise`.
2. Editar `tracker.config.json` con:
   ```json
   {
       "localFileOutputMode": "Custom",
       "customOutputDir": "\\\\server\\telemetry\\feather_rise"
   }
   ```
3. Distribuir la build y el config juntos. Cada sesión escribe un archivo independiente (el `session_id` garantiza que no colisionan).

### Build de release sin telemetría

Distribuir junto con la build un `tracker.config.json` con:
```json
{ "enabled": false }
```
El tracker no se inicializa y el juego corre exactamente igual pero sin recoger ni escribir nada.

### Solo quiero probar la mecánica de plumas

Desactivar el resto de eventos para reducir el ruido en el análisis:
```json
{
    "disabledEventTypes": [
        "Player_Attack",
        "Chest_Opened"
    ]
}
```

### Sesiones muy largas (1+ hora)

Activar rotación para evitar archivos gigantes:
```json
{ "fileRotationMaxMb": 10.0 }
```
Cuando un archivo pasa de 10 MB, se cierra y se abre `_part002.json`. El script Python los procesa automáticamente.

### Envío a Firebase en lugar de disco

```json
{
    "persistence": "Firebase",
    "firebaseDatabaseUrl": "https://tu-proyecto.europe-west1.firebasedatabase.app/"
}
```
El serializador se cambia automáticamente al adecuado para Firebase.

---

## 7. Qué hacer con las trazas

El sistema genera archivos `.json` o `.csv` con todos los eventos de la sesión. Para convertirlos en métricas y gráficas:

1. Copia los archivos de `{carpeta_de_salida}/Telemetry/` a la carpeta `analisis/data/` del repo.
2. Ejecuta:
   ```bash
   cd analisis
   python analyze_telemetry.py
   ```
3. Revisa los resultados en `analisis/output/`:
   - `metrics_summary.json` — todas las métricas calculadas
   - `metrics_summary.md` — resumen legible
   - `figures/` — gráficas PNG (una por métrica)

Ver el `README.md` de la carpeta `analisis/` para más detalles del pipeline de análisis.

---

## 8. Resolución de problemas

**No aparece la carpeta `Telemetry/` junto al ejecutable.**
Revisa la consola de Unity. El log `[TrackerInitializer] Trazas locales en: …` te dice la ruta real que está usando. Si dice otra ruta, comprueba `localFileOutputMode` en el config.

**El JSON generado termina con una coma colgando.**
Pasaba en versiones antiguas del tracker si el juego crasheaba antes de `OnApplicationQuit`. Ya está resuelto: cada flush fuerza una escritura a disco, y el cierre de sesión normal escribe el footer `]`. Si aún así ves un archivo malformado, el script Python de análisis lo parsea tolerantemente igualmente.

**El playtester dice que no se genera el fichero.**
1. ¿Tiene permisos de escritura en la carpeta del ejecutable? Si no, cambiar a `localFileOutputMode: "PersistentData"`.
2. ¿El config está mal formado? El tracker hace fallback a los valores del Inspector si no puede leer el config. Revisa la consola.

**Demasiados logs en la consola.**
Poner `"verboseLogging": false` (default). Solo activarlo para depurar.

**Quiero usar una ruta de salida que contiene espacios.**
No hay problema. En Windows, en el JSON escribir las barras dobles:
```json
{ "customOutputDir": "C:\\Ruta con espacios\\Telemetry" }
```
o con barras normales:
```json
{ "customOutputDir": "C:/Ruta con espacios/Telemetry" }
```

**El tracker afecta al rendimiento.**
No debería: la serialización corre en un `Task` secundario y el `FileStream` está buffereado. Si en tu PC concreto hay *stuttering*, probar a subir `autoFlushIntervalSeconds` (menos flushes) o `fileBufferSizeBytes` (menos I/O real). Si persiste, mirar con el Profiler dónde pasa el tiempo.

**El juego se cuelga en el quit.**
Probablemente estás escribiendo a una ruta lenta (disco de red o unidad externa con fallo). El tracker hace un flush síncrono en el `OnApplicationQuit` para no perder datos, y si la escritura tarda lo bloquea. Soluciones: cambiar a una ruta local o reducir el tamaño de la cola subiendo la frecuencia de flushes.