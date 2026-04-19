using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Configuración del sistema de telemetría.
/// Se puede cargar desde un fichero JSON (para que sea editable en BUILD
/// sin recompilar) o usar los valores por defecto del Inspector como fallback.
///
/// Orden de prioridad al cargar:
///   1. Fichero JSON junto al ejecutable: {rutaBase}/tracker.config.json
///   2. Fichero JSON en persistentDataPath:  {persistentDataPath}/tracker.config.json
///   3. Valores del Inspector del TrackerInitializer (si no se encuentra fichero).
/// </summary>
[Serializable]
public class TrackerConfig
{
    // ---------------- Flags maestros ----------------

    /// <summary>
    /// Si es false, el tracker no se inicializa y no se recogen eventos.
    /// Permite desactivar la telemetría por completo desde el config sin tocar código.
    /// </summary>
    public bool enabled = true;

    /// <summary>
    /// Si es true, vuelca al Debug.Log cada evento encolado y cada flush.
    /// Recomendado dejarlo en false en builds de producción para no saturar el log.
    /// </summary>
    public bool verboseLogging = false;

    // ---------------- Serialización ----------------

    /// <summary>Formato de serialización: "JSON" o "CSV".</summary>
    public string serializer = "JSON";

    // ---------------- Persistencia ----------------

    /// <summary>Destino de las trazas: "LocalFile" o "Firebase".</summary>
    public string persistence = "LocalFile";

    /// <summary>
    /// Modo de resolución de la carpeta de salida para LocalFilePersistence:
    ///   - "NextToExe":      carpeta Telemetry/ junto al ejecutable (editor: junto a Assets/).
    ///                       Más fácil de compartir y de encontrar.
    ///   - "PersistentData": usa Application.persistentDataPath
    ///                       (en Windows: %APPDATA%/LocalLow/{company}/{product}).
    ///   - "Custom":         usa la ruta absoluta definida en `customOutputDir`.
    /// </summary>
    public string localFileOutputMode = "NextToExe";

    /// <summary>
    /// Ruta absoluta personalizada, solo usada si `localFileOutputMode == "Custom"`.
    /// Si la carpeta no existe, el tracker la crea.
    /// </summary>
    public string customOutputDir = "";

    /// <summary>
    /// Tamaño máximo del archivo de traza en MB antes de rotar a uno nuevo.
    /// 0 o negativo = sin rotación (un único archivo por sesión, comportamiento clásico).
    /// </summary>
    public float fileRotationMaxMb = 0f;

    /// <summary>URL base de la Realtime Database de Firebase (solo si persistence == "Firebase").</summary>
    public string firebaseDatabaseUrl = "";

    // ---------------- Flush / rendimiento ----------------

    /// <summary>Intervalo en segundos entre flushes automáticos de la cola a disco/red.</summary>
    public float autoFlushIntervalSeconds = 30f;

    /// <summary>
    /// Tamaño máximo del buffer interno del FileStream en bytes (4096 por defecto).
    /// Un buffer más grande reduce I/O al disco pero aumenta la pérdida ante crash.
    /// </summary>
    public int fileBufferSizeBytes = 4096;

    // ---------------- Filtrado de eventos ----------------

    /// <summary>
    /// Lista de nombres de clase de eventos a IGNORAR.
    /// Los eventos cuyo `event_type` esté en esta lista se descartan antes de encolarse.
    /// Ejemplo: ["Player_Attack", "Feather_Recall_Attempt"] para desactivar esos dos.
    /// </summary>
    public List<string> disabledEventTypes = new List<string>();

    // ---------------- Carga / guardado ----------------

    /// <summary>Nombre del fichero de configuración que se busca al arrancar.</summary>
    public const string CONFIG_FILE_NAME = "tracker.config.json";

    /// <summary>
    /// Carga la configuración del sistema de ficheros siguiendo el orden de prioridad.
    /// Devuelve los defaults si no encuentra nada (en cuyo caso `loadedFromPath` queda en null).
    /// </summary>
    public static TrackerConfig Load(out string loadedFromPath)
    {
        // Prioridad 1: junto al ejecutable
        string nextToExe = Path.Combine(GetDirectoryNextToExe(), CONFIG_FILE_NAME);
        if (File.Exists(nextToExe))
        {
            TrackerConfig cfg = ReadFromFile(nextToExe);
            if (cfg != null) { loadedFromPath = nextToExe; return cfg; }
        }

        // Prioridad 2: persistentDataPath
        string persistent = Path.Combine(Application.persistentDataPath, CONFIG_FILE_NAME);
        if (File.Exists(persistent))
        {
            TrackerConfig cfg = ReadFromFile(persistent);
            if (cfg != null) { loadedFromPath = persistent; return cfg; }
        }

        loadedFromPath = null;
        return null;
    }

    /// <summary>
    /// Escribe la configuración actual al fichero `tracker.config.json` junto al
    /// ejecutable. Útil como herramienta de depuración (se llama desde el
    /// TrackerInitializer con un botón en el Inspector) para generar una plantilla
    /// que luego edita el playtester.
    /// </summary>
    public void SaveToFileNextToExe(out string savedPath)
    {
        string dir = GetDirectoryNextToExe();
        Directory.CreateDirectory(dir);
        savedPath = Path.Combine(dir, CONFIG_FILE_NAME);
        File.WriteAllText(savedPath, JsonUtility.ToJson(this, true));
    }

    // ---------------- Helpers ----------------

    private static TrackerConfig ReadFromFile(string path)
    {
        try
        {
            string raw = File.ReadAllText(path);
            return JsonUtility.FromJson<TrackerConfig>(raw);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackerConfig] Error leyendo {path}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Devuelve la carpeta del ejecutable en build, o la carpeta del proyecto en
    /// editor (un nivel por encima de Assets/).
    /// </summary>
    public static string GetDirectoryNextToExe()
    {
        // Application.dataPath apunta a:
        //   - En build Windows/Linux/Mac:  {ruta}/{producto}_Data
        //   - En editor:                   {proyecto}/Assets
        // Subir un nivel nos deja junto al ejecutable (o en la raíz del proyecto).
        string dataPath = Application.dataPath;
        return Path.GetFullPath(Path.Combine(dataPath, ".."));
    }

    /// <summary>
    /// Resuelve a ruta absoluta la carpeta de salida según el modo configurado.
    /// Crea la carpeta si no existe.
    /// </summary>
    public string ResolveLocalOutputDir()
    {
        string dir;
        switch (localFileOutputMode)
        {
            case "PersistentData":
                dir = Path.Combine(Application.persistentDataPath, "Telemetry");
                break;
            case "Custom":
                if (string.IsNullOrWhiteSpace(customOutputDir))
                {
                    Debug.LogWarning("[TrackerConfig] customOutputDir está vacío, "
                                     + "usando NextToExe como fallback.");
                    dir = Path.Combine(GetDirectoryNextToExe(), "Telemetry");
                }
                else
                {
                    dir = customOutputDir;
                }
                break;
            case "NextToExe":
            default:
                dir = Path.Combine(GetDirectoryNextToExe(), "Telemetry");
                break;
        }
        Directory.CreateDirectory(dir);
        return dir;
    }
}