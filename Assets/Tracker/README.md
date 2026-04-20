# Arquitectura del Sistema de Telemetría (Tracker)

Este documento detalla el diseño técnico y la arquitectura final del sistema de telemetría (Tracker) integrado en *Feather Rise*. El sistema ha sido diseñado como un módulo robusto, independiente, altamente configurable y preparado para no afectar al rendimiento del videojuego mediante el uso de concurrencia y operaciones asíncronas.

---

## 1. Visión General de la Arquitectura

El Tracker sigue un patrón modular basado en la inyección de dependencias y la separación de responsabilidades. Se compone de los siguientes bloques principales:

1. **Configuración Dinámica (`TrackerConfig` y `TrackerInitializer`)**: Permite alterar el comportamiento del sistema (rutas, formatos, habilitar/deshabilitar eventos) sin necesidad de recompilar el juego.
2. **Núcleo Central (`Tracker.cs`)**: Gestiona la ingesta de eventos mediante una cola concurrente (`ConcurrentQueue`) y el volcado periódico (*flush*) en hilos secundarios.
3. **Jerarquía de Eventos**: Clases de datos estructuradas que heredan de una base común para automatizar la captura de metadatos.
4. **Serialización y Persistencia (Interfaces)**: Capas abstractas que dictan cómo se formatean y dónde se guardan los datos, permitiendo intercambiar módulos (ej. JSON vs CSV, Local vs Nube) sin modificar el núcleo.

---

## 2. Inicialización y Configuración Dinámica

Para facilitar el *playtesting*, el sistema se inicializa evaluando un archivo de configuración en formato JSON (`tracker.config.json`). Si este archivo existe junto al ejecutable, sobrescribe los valores del Inspector.

Esto permite modificar la salida de datos sin tocar el código:
```csharp
// Fragmento de TrackerConfig.cs
public class TrackerConfig
{
    public bool enabled = true;
    public string serializer = "JSON";       // "JSON" o "CSV"
    public string persistence = "LocalFile"; // "LocalFile" o "Firebase"
    public float fileRotationMaxMb = 0f;
    public List<string> disabledEventTypes = new List<string>(); // Lista negra de eventos
}
```
El `TrackerInitializer` lee esta configuración, instancia los serializadores y persistencias correspondientes, y hace la inyección de dependencias en el Tracker.

---

## 3. Módulo Central (`Tracker.cs`)

Centraliza el flujo de datos protegiendo el *framerate* del juego. 

### 3.1. Ingesta No Bloqueante
Cuando el juego llama a `TrackEvent(e)`, el núcleo evalúa primero si ese evento está en la lista negra (`disabledEventTypes`) con complejidad O(1). Si es válido, se inyecta el `timestamp` y se introduce en una `ConcurrentQueue`, garantizando la seguridad entre hilos (*thread-safe*).

### 3.2. Flush Asíncrono y Síncrono
El volcado de datos (*flush*) extrae los eventos de la cola y los manda a guardar. Se puede ejecutar de dos formas:
* **Asíncrono (Auto-Flush):** Una corutina lo ejecuta cada X segundos usando un hilo del sistema (`Task.Run`), evitando bloquear el renderizado.
* **Síncrono (Preventivo):** Se fuerza en el hilo principal durante eventos críticos del SO (`OnApplicationQuit`, `OnApplicationPause`) para evitar pérdida de datos si el juego se cierra de golpe.

```csharp
// Fragmento de Tracker.cs - Lógica principal de volcado
public void Flush(bool forceSynchronous = false)
{
    // [...] Extracción de la cola a la lista 'batch'
    string data = _serializer.Serialize(batch, _isFirstFlush);
    
    if (forceSynchronous) {
        _persistence.Save(data); // Escribe en el hilo principal
    } else {
        Task.Run(() => {
            _persistence.Save(data); // Escribe en un hilo secundario
        });
    }
}
```

---

## 4. Jerarquía y Estructura de Eventos

Todos los datos se estructuran bajo un modelo de herencia.

### 4.1. Clase Base (`TrackerEvent.cs`)
Define el esqueleto obligatorio. Captura automáticamente el nombre de la clase hija mediante reflexión para el campo `event_type`.
```csharp
[System.Serializable]
public abstract class TrackerEvent
{
    public long timestamp;
    public string session_id;
    public string event_type;

    public TrackerEvent()
    {
        this.event_type = this.GetType().Name; 
    }
}
```

---

## 5. Capa de Serialización (`ISerializer`)

Transforma los objetos de C# en cadenas de texto formateadas. Diseñado mediante interfaz (`Serialize`, `GetHeader`, `GetFooter`) para permitir múltiples salidas.

### 5.1. `JSONSerializer.cs`
Genera un archivo JSON válido en formato de "lista de listas". Cada *flush* inyecta un nuevo array de eventos.
```csharp
// Fragmento JSONSerializer.cs
if (!isFirstBatch) sb.AppendLine(","); // Separa los batches
sb.Append("  [");
// [...] bucle JsonUtility.ToJson(events[i])
sb.Append("  ]");
```

### 5.2. `CSVSerializer.cs`
Genera un formato híbrido muy eficiente para análisis de datos. Crea columnas fijas para los metadatos y empaca los datos específicos del evento en un payload JSON, usando reflexión para ignorar los campos padre.
```csharp
// Fragmento CSVSerializer.cs
    string baseCols = $"{e.timestamp},{e.session_id},{e.event_type},";

    // Sacamos solo los campos ESPECÍFICOS del evento (no los de la clase base)
    string specificJson = SerializeSpecificFields(e);

    // Escapar comillas dobles duplicándolas (regla CSV estándar)
    string escaped = specificJson.Replace("\"", "\"\"");

    sb.Append(baseCols).Append('"').Append(escaped).Append('"').AppendLine();
```

### 5.3. `FirebaseSerializer.cs`
Adaptado para la API REST de Firebase Realtime Database. Genera un array plano de objetos JSON sin cabeceras ni separadores entre *batches*.

---

## 6. Capa de Persistencia (`IPersistence`)

Encargada del I/O (Input/Output) físico o de red. Recibe el texto ya serializado y lo escribe en su destino.

### 6.1. `LocalFilePersistence.cs`
Módulo robusto de guardado en disco local. En lugar de abrir y cerrar el archivo en cada frame, **mantiene un `FileStream` abierto** durante toda la partida, mejorando el rendimiento masivamente.
* Usa `Flush(flushToDisk: true)` para forzar al sistema operativo a escribir los datos inmediatamente, mitigando pérdidas por *crashes*.
* Soporta rotación de archivos por tamaño.
* Usa `FileShare.Read` para permitir lectura simultánea por scripts de análisis.

```csharp
// Fragmento LocalFilePersistence.cs - Guardado ultra-rápido y seguro
public void Save(string data)
{
    if (_rotationMaxBytes > 0 && _stream.Length >= _rotationMaxBytes)
        RotateToNewPart(); // Rotación automática si el archivo es muy grande

    _writer.Write(data);
    _writer.Flush();
    _stream.Flush(flushToDisk: true); // Forzado seguro a disco
}
```

### 6.2. `FirebasePersistence.cs`
Envía los datos a la nube utilizando llamadas REST. Reutiliza una única instancia estática de `HttpClient` (thread-safe) para no agotar los *sockets* del sistema. Como el Tracker lo invoca dentro de un `Task.Run`, puede esperar la respuesta HTTP de forma síncrona (`.Result`) sin congelar el videojuego.

```csharp
// Fragmento FirebasePersistence.cs
public void Save(string data)
{
    var content = new StringContent(data, Encoding.UTF8, "application/json");
    
    // Al estar en un hilo secundario, .Result no bloquea a Unity
    HttpResponseMessage response = _httpClient.PostAsync(_databaseUrl, content).Result;

    if (!response.IsSuccessStatusCode)
        throw new System.Exception($"Error de red: {response.StatusCode}");
}
```
