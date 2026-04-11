# FASE 2: Arquitectura del Sistema de Telemetría (Tracker)

Este documento detalla el diseño técnico y la arquitectura del sistema de telemetría (Tracker) que se integrará en Feather Rise. El sistema ha sido diseñado como un módulo independiente y desacoplado, garantizando que su funcionamiento no afecte al rendimiento del juego y que, en caso de fallo, el flujo de la partida continúe con normalidad.

## 1. Visión General de la Arquitectura

La arquitectura del Tracker sigue un patrón modular basado en tres pilares fundamentales:

* Gestión de Eventos: Generación y almacenamiento temporal en una cola concurrente.

* Serialización: Transformación de los objetos en memoria a un formato de datos estructurado.

* Persistencia: Guardado físico de los datos serializados.

Para garantizar la extensibilidad futura (tal y como se exige en los requisitos), las capas de serialización y persistencia se comunicarán a través de Interfaces, permitiendo cambiar el formato (ej. de JSON a XML) o el destino (ej. de Local a un Servidor en la nube) sin modificar el núcleo del Tracker.

## 2. Jerarquía y Estructura de Eventos

Todos los eventos del sistema heredarán de una clase base común para garantizar la consistencia en los datos obligatorios.

### 2.1. Clase Base (TrackerEvent)

Atributos fijos que el Tracker asigna automáticamente a todos los eventos en el momento de su creación.
``` c#
[System.Serializable]
public abstract class TrackerEvent
{
    public long timestamp;
    public string session_id;
    public string event_type;

    public TrackerEvent()
    {
        // El tracker asigará el timestamp y session_id al encolar
        this.event_type = this.GetType().Name; 
    }
}

```
### 2.2. Eventos de Sistema (Obligatorios)

Eventos requeridos por el diseño general de la práctica para estructurar el flujo de datos:

* **`Session_Start`** : Marca el inicio de la ejecución del juego.

* **`Session_End`** : Marca el cierre del juego o de la sesión.

* **`Level_Start`** : Lanzado al cargar una escena de nivel. Atributo: level_id.

* **`Level_End `**: Lanzado al terminar un nivel. Atributos: level_id, result (Enum: "Victory", "Defeat", "Quit").

### 2.3. Eventos Personalizados (Definidos en Fase 1)

Eventos específicos instrumentalizados en el código de Feather Rise. Se implementan heredando de TrackerEvent:
``` c#
[System.Serializable]
public class Feather_Recall_Attempt : TrackerEvent
{
    public bool is_successful;
    
    public Feather_Recall_Attempt(bool is_successful)
    {
        this.is_successful = is_successful;
    }
}

[System.Serializable]
public class Player_Death : TrackerEvent
{
    public float pos_x;
    public float pos_y;
    public string cause_of_death;
    public string level_id;

    public Player_Death(float x, float y, string cause, string level)
    {
        this.pos_x = x;
        this.pos_y = y;
        this.cause_of_death = cause;
        this.level_id = level;
    }
}

// ... Resto de eventos: Chest_Opened, Player_Attack, Checkpoint_Reached
```

## 3. Módulo Central (Tracker Core)

El núcleo del Tracker se implementará como un Singleton persistente (MonoBehaviour con DontDestroyOnLoad en Unity o clase estática) para ser accesible desde cualquier script.

Flujo de trabajo:

* **Track**: El juego invoca al método TrackEvent(TrackerEvent e). El evento se encola inmediatamente en una estructura de datos (ej. una lista o cola tipo FIFO) en memoria RAM. No se procesa en el acto para evitar caídas de frames.

* **Flush** (Vaciado): De forma periódica (ej. cada X segundos, al terminar un nivel, o al cerrar el juego), se llama al método Flush(). Este método extrae los eventos de la cola en lotes, los pasa al Serializador y posteriormente al módulo de Persistencia.
``` c#
public class Tracker 
{
    private static Tracker _instance;
    public static Tracker Instance { get { /* Singleton logic */ return _instance; } }

    private Queue<TrackerEvent> eventQueue = new Queue<TrackerEvent>();
    private string currentSessionId;
    
    private ISerializer serializer;
    private IPersistence persistence;

    public void Init(ISerializer ser, IPersistence per) 
    {
        this.serializer = ser;
        this.persistence = per;
        this.currentSessionId = System.Guid.NewGuid().ToString();
        // Disparar evento de inicio de sesión
        TrackEvent(new Session_Start()); 
    }

    public void TrackEvent(TrackerEvent e)
    {
        // Asignar datos automáticos
        e.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        e.session_id = currentSessionId;
        
        // Encolar rápidamente sin bloquear
        eventQueue.Enqueue(e);
    }

    public void Flush()
    {
        if (eventQueue.Count == 0) return;

        // Pasar la cola a una lista para procesar
        List<TrackerEvent> eventsToFlush = new List<TrackerEvent>();
        while(eventQueue.Count > 0) {
            eventsToFlush.Add(eventQueue.Dequeue());
        }

        // Serializar y guardar (protegido contra fallos)
        try {
            string data = serializer.Serialize(eventsToFlush);
            persistence.Save(data);
        } catch (System.Exception ex) {
            UnityEngine.Debug.LogError("Tracker Error: " + ex.Message);
            // Opcional: Devolver eventos a la cola si falla el guardado
        }
    }
}
```

## 4. Sistema de Serialización

El módulo encargado de traducir los objetos a texto plano. Se basará en la interfaz ISerializer.

```c#
public interface ISerializer 
{
    string Serialize(List<TrackerEvent> events);
}
```

Implementación Actual (JsonSerializer):
Siguiendo los requisitos de la práctica, se implementará un serializador que convierta las listas de eventos a formato JSON (usando utilidades como JsonUtility de Unity o Newtonsoft.Json). Esto permitirá un análisis directo y automatizado con scripts de Python en la Fase 4.

## 5. Sistema de Persistencia

El módulo encargado de dar salida a los datos serializados, basado en la interfaz IPersistence.
``` c#
public interface IPersistence 
{
    void Save(string serializedData);
    void Close();
}
```

* **Implementación Actual** (LocalFilePersistence):
Tal y como requiere el enunciado, los datos se volcarán en el disco duro local del equipo.

* **Estrategia de archivos**: Se creará un archivo distinto por cada sesión de juego. El nombre del archivo incluirá el session_id y el timestamp inicial (ej. telemetry_session_5f8a_20260411.json).

* **Seguridad** (Fail-safe): Toda operación de entrada/salida (I/O) al disco estará encapsulada en bloques try-catch para garantizar que un error de permisos o de disco lleno no bloquee ni crashee el videojuego principal.

## 6. Instrumentalización (Integración con el Juego)

Para instrumentalizar Feather Rise, se modificarán las clases clave (ej. PlayerController, HealthSystem, ChestInteractable) insertando llamadas asíncronas o de bajo coste al Tracker.

Ejemplo de uso previsto en el código del juego:
```c#
// Ejemplo en el componente HealthSystem.cs
public class HealthSystem : MonoBehaviour
{
    void Die() 
    {
        // Lógica del juego... (animaciones, recarga de escena, etc.)
        
        // Llamada de instrumentalización al Tracker
        Tracker.Instance.TrackEvent(new Player_Death(
            transform.position.x, 
            transform.position.y, 
            "Spikes", 
            "2.2"
        ));
    }
}
```