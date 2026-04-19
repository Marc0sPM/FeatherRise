using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bootstrap del sistema de telemetría. Se ejecuta al arranque y decide
/// cómo configurar el Tracker basándose en:
///
///   1. Un fichero `tracker.config.json` si existe (junto al ejecutable o
///      en persistentDataPath). Permite al playtester cambiar la
///      configuración de una build SIN recompilar.
///
///   2. En su defecto, los valores que tengas puestos en el Inspector
///      (equivalente a la configuración anterior, retrocompatible).
///
/// En el editor, el botón contextual "Generate config file next to exe"
/// vuelca los valores actuales del Inspector a un fichero JSON que luego
/// puedes distribuir junto con la build.
/// </summary>
public class TrackerInitializer : MonoBehaviour
{
    public enum S_type { JSON, CSV };
    public enum P_type { LOCAL_FILE, FIREBASE }
    public enum OutputMode { NextToExe, PersistentData, Custom }

    [Header("--- Fallback (usado si NO existe tracker.config.json) ---")]
    [Tooltip("Si hay un fichero tracker.config.json junto al ejecutable o en "
             + "persistentDataPath, estos valores se IGNORAN y se usan los del fichero.")]
    public bool enabled = true;
    public bool verboseLogging = false;

    [Header("Serialización y persistencia")]
    public S_type serializerType = S_type.JSON;
    public P_type persistanceType = P_type.LOCAL_FILE;

    [Header("Salida de ficheros locales")]
    [Tooltip("Dónde guardar los .json/.csv de telemetría.\n"
             + "• NextToExe: carpeta Telemetry/ junto al ejecutable (recomendado).\n"
             + "• PersistentData: %APPDATA%/LocalLow/... (comportamiento antiguo).\n"
             + "• Custom: la ruta que escribas en 'customOutputDir'.")]
    public OutputMode localFileOutputMode = OutputMode.NextToExe;

    [Tooltip("Solo usado si localFileOutputMode = Custom")]
    public string customOutputDir = "";

    [Tooltip("Tamaño máximo por archivo en MB antes de rotar (0 = sin rotación).")]
    public float fileRotationMaxMb = 0f;

    [Header("Servidor Firebase (si persistencia = FIREBASE)")]
    public string firebaseDatabaseUrl =
        "https://featherrise-telemetry-p3-default-rtdb.europe-west1.firebasedatabase.app/";

    [Header("Rendimiento")]
    [Tooltip("Intervalo en segundos entre flushes automáticos.")]
    public float autoFlushIntervalSeconds = 30f;

    [Tooltip("Buffer interno del FileStream en bytes.")]
    public int fileBufferSizeBytes = 4096;

    [Header("Eventos desactivados")]
    [Tooltip("Nombres de clase de eventos que NO se registrarán (ej: Player_Attack).")]
    public List<string> disabledEventTypes = new List<string>();

    // ---------------------- ciclo de vida ----------------------

    void Start()
    {
        // 1. Cargar configuración
        TrackerConfig config = TrackerConfig.Load(out string loadedPath);
        if (config == null)
        {
            config = BuildConfigFromInspector();
            Debug.Log("[TrackerInitializer] No se encontró tracker.config.json, "
                      + "usando valores del Inspector.");
        }
        else
        {
            Debug.Log($"[TrackerInitializer] Configuración cargada desde: {loadedPath}");
        }

        // 2. Si el tracker está desactivado, no construimos dependencias
        if (!config.enabled)
        {
            Tracker.Instance.Init(null, null, "", config);
            return;
        }

        // 3. Construir serializador y persistencia según config
        string sessionId = System.Guid.NewGuid().ToString();
        string fileExtension;
        ISerializer ser = ChooseSerializer(config, out fileExtension);
        IPersistence pers = ChoosePersistence(config, sessionId, fileExtension);

        // 4. Transferir el intervalo al Tracker vía config (ya está en él,
        //    pero el Tracker.autoFlushInterval viejo ya no se usa)
        Tracker.Instance.Init(ser, pers, sessionId, config);
    }

    /// <summary>
    /// Copia los valores del Inspector a una instancia de TrackerConfig.
    /// Usado como fallback cuando no hay fichero de configuración.
    /// </summary>
    private TrackerConfig BuildConfigFromInspector()
    {
        return new TrackerConfig
        {
            enabled = enabled,
            verboseLogging = verboseLogging,
            serializer = serializerType.ToString(),
            persistence = persistanceType == P_type.LOCAL_FILE ? "LocalFile" : "Firebase",
            localFileOutputMode = localFileOutputMode.ToString(),
            customOutputDir = customOutputDir,
            fileRotationMaxMb = fileRotationMaxMb,
            firebaseDatabaseUrl = firebaseDatabaseUrl,
            autoFlushIntervalSeconds = autoFlushIntervalSeconds,
            fileBufferSizeBytes = fileBufferSizeBytes,
            disabledEventTypes = new List<string>(disabledEventTypes ?? new List<string>()),
        };
    }

    private ISerializer ChooseSerializer(TrackerConfig cfg, out string extension)
    {
        // Firebase siempre usa su propio serializador (el JSON plano de
        // Firebase Realtime DB, sin comas entre batches).
        if (cfg.persistence == "Firebase")
        {
            extension = ".json"; // no se usa realmente
            return new FirebaseSerializer();
        }

        switch (cfg.serializer?.ToUpperInvariant())
        {
            case "CSV":
                extension = ".csv";
                return new CSVSerializer();
            case "JSON":
            default:
                extension = ".json";
                return new JSONSerializer();
        }
    }

    private IPersistence ChoosePersistence(TrackerConfig cfg, string sessionId,
                                           string extension)
    {
        if (cfg.persistence == "Firebase")
        {
            if (string.IsNullOrWhiteSpace(cfg.firebaseDatabaseUrl))
            {
                Debug.LogWarning("[TrackerInitializer] Firebase URL vacía, cayendo a LocalFile.");
            }
            else
            {
                return new FirebasePersistence(cfg.firebaseDatabaseUrl, sessionId);
            }
        }

        string outDir = cfg.ResolveLocalOutputDir();
        Debug.Log($"[TrackerInitializer] Trazas locales en: {outDir}");
        return new LocalFilePersistence(outDir, sessionId, extension,
                                        cfg.fileRotationMaxMb,
                                        cfg.fileBufferSizeBytes);
    }

    // ---------------------- utilidad de editor ----------------------

    [ContextMenu("Generate config file next to exe (with current Inspector values)")]
    public void GenerateConfigFile()
    {
        TrackerConfig cfg = BuildConfigFromInspector();
        cfg.SaveToFileNextToExe(out string savedPath);
        Debug.Log($"[TrackerInitializer] Config escrito en: {savedPath}\n"
                  + "Puedes distribuir este fichero junto con la build. El usuario "
                  + "podrá editarlo para cambiar la configuración sin recompilar.");
    }
}