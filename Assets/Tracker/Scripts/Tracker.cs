using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Tracker de telemetría (singleton). Gestiona la cola de eventos, el flush
/// periódico a disco/red y la gestión del ciclo de vida del juego.
/// </summary>
public class Tracker : MonoBehaviour
{
    public static Tracker Instance { get; private set; }

    private ISerializer _serializer;
    private IPersistence _persistence;
    private bool _isFirstFlush = true;

    private ConcurrentQueue<TrackerEvent> _eventQueue = new ConcurrentQueue<TrackerEvent>();
    private string _currentSessionId;

    /// <summary>Configuración activa (nunca null tras Init, se aplican defaults si hace falta).</summary>
    private TrackerConfig _config;

    /// <summary>Lookup set O(1) de eventos deshabilitados, se construye en Init().</summary>
    private HashSet<string> _disabledEventTypes = new HashSet<string>();

    /// <summary>Flag de parada para la corutina de autoflush.</summary>
    private bool _autoFlushRunning;

    /// <summary>Indica si el tracker está inicializado y aceptando eventos.</summary>
    public bool IsReady { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// Inicializa el Tracker con las dependencias y la configuración.
    /// Si config.enabled == false, el tracker se queda inactivo y TrackEvent
    /// se convierte en un no-op silencioso (el juego sigue funcionando).
    /// </summary>
    public void Init(ISerializer ser, IPersistence pers, string sessionId,
                     TrackerConfig config)
    {
        _config = config ?? new TrackerConfig();

        if (!_config.enabled)
        {
            Debug.Log("[TRACKER] Desactivado por configuración (enabled=false).");
            IsReady = false;
            return;
        }

        _serializer = ser;
        _persistence = pers;
        _currentSessionId = sessionId;

        // Construimos el HashSet de eventos filtrados para lookup O(1)
        _disabledEventTypes.Clear();
        if (_config.disabledEventTypes != null)
        {
            foreach (string t in _config.disabledEventTypes)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    _disabledEventTypes.Add(t.Trim());
            }
        }
        if (_disabledEventTypes.Count > 0)
        {
            Debug.Log($"[TRACKER] Eventos deshabilitados: "
                      + $"{string.Join(", ", _disabledEventTypes)}");
        }

        Debug.Log($"[TRACKER] Inicializado. Sesion ID: {_currentSessionId} · "
                  + $"Flush cada {_config.autoFlushIntervalSeconds}s");

        _persistence.Open(_serializer.GetHeader());
        TrackEvent(new Session_Start());

        _autoFlushRunning = true;
        StartCoroutine(AutoFlushCoroutine());
        IsReady = true;
    }

    /// <summary>
    /// Encola un evento. Aplica el filtro de `disabledEventTypes` antes de
    /// encolar: los eventos deshabilitados se descartan de forma silenciosa
    /// sin consumir memoria ni I/O.
    /// </summary>
    public void TrackEvent(TrackerEvent e)
    {
        if (!IsReady || e == null) return;

        string evType = e.GetType().Name;
        if (_disabledEventTypes.Contains(evType))
        {
            // Descartado por filtro. Sin log (sería ruido: lo pediste al config).
            return;
        }

        e.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        e.session_id = _currentSessionId;
        _eventQueue.Enqueue(e);

        if (_config.verboseLogging)
        {
            Debug.Log($"[TRACKER] +{evType}  (cola: {_eventQueue.Count})");
        }
    }

    /// <summary>
    /// Procesa la cola, serializa y persiste. Si forceSynchronous == true,
    /// la escritura se hace en el hilo principal (uso típico: OnApplicationQuit,
    /// donde Windows mata los hilos secundarios antes de que terminen). En
    /// caso contrario se hace en un Task para no bloquear el render.
    ///
    /// Ya no hay doble Save(): se elige UN camino (sync o async) y se
    /// ejecuta exactamente una vez. Si falla, los eventos se reencolan.
    /// </summary>
    public void Flush(bool forceSynchronous = false)
    {
        if (!IsReady) return;
        if (_eventQueue.IsEmpty) return;

        // Vaciamos la cola a una lista local (única sección crítica)
        List<TrackerEvent> batch = new List<TrackerEvent>();
        while (_eventQueue.TryDequeue(out TrackerEvent e))
        {
            batch.Add(e);
        }
        if (batch.Count == 0) return;

        // Serializamos en el hilo que llama (barato, no hace I/O)
        string data;
        try
        {
            data = _serializer.Serialize(batch, _isFirstFlush);
            _isFirstFlush = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TRACKER] Error al serializar: {ex.Message}");
            ReturnEventsToQueue(batch);
            return;
        }

        if (_config.verboseLogging)
        {
            Debug.Log($"[TRACKER] Flush de {batch.Count} eventos "
                      + $"(modo: {(forceSynchronous ? "sync" : "async")})");
        }

        if (forceSynchronous)
        {
            // Síncrono: escribimos en este hilo. Única invocación de Save.
            try
            {
                _persistence.Save(data);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TRACKER] Error sync al guardar: {ex.Message}");
                ReturnEventsToQueue(batch);
            }
        }
        else
        {
            // Asíncrono: el hilo del pool escribe. Única invocación de Save.
            Task.Run(() =>
            {
                try
                {
                    _persistence.Save(data);
                }
                catch (System.Exception ex)
                {
                    // No podemos usar Debug.LogError fuera del hilo principal de
                    // forma segura en todas las plataformas; usamos LogError que
                    // sí es thread-safe en Unity moderno.
                    Debug.LogError($"[TRACKER] Error async al guardar: {ex.Message}");
                    ReturnEventsToQueue(batch);
                }
            });
        }
    }

    private void ReturnEventsToQueue(List<TrackerEvent> failedEvents)
    {
        foreach (var e in failedEvents) _eventQueue.Enqueue(e);
    }

    /// <summary>
    /// Corutina de autoflush. Usa un flag booleano para salir limpiamente
    /// (vs. el anterior `while(true)` que no paraba nunca).
    /// </summary>
    private IEnumerator AutoFlushCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_config.autoFlushIntervalSeconds);
        while (_autoFlushRunning)
        {
            yield return wait;
            if (_autoFlushRunning) Flush(false);
        }
    }

    /// <summary>
    /// Al minimizar en desktop el SO puede matar el proceso sin
    /// avisar. Este callback se dispara antes, así que forzamos un flush
    /// síncrono preventivo para no perder el último batch.
    /// </summary>
    private void OnApplicationPause(bool paused)
    {
        if (!IsReady) return;
        if (paused)
        {
            if (_config.verboseLogging)
                Debug.Log("[TRACKER] Aplicación pausada, flush preventivo.");
            Flush(true);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!IsReady) return;
        if (!hasFocus)
        {
            // Flush preventivo al perder el foco: si el usuario cierra la
            // ventana sin salir por menú, al menos recuperamos lo que haya
            // en cola hasta este momento.
            Flush(true);
        }
    }

    private void OnApplicationQuit()
    {
        if (!IsReady) return;
        TrackEvent(new Session_End());
        Flush(true);

        _autoFlushRunning = false;

        if (_persistence != null)
        {
            try { _persistence.Close(_serializer.GetFooter()); }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TRACKER] Error cerrando persistencia: {ex.Message}");
            }
        }
        IsReady = false;
    }

    private void OnDisable()
    {
        // Red de seguridad: si el GameObject se desactiva sin quit, paramos
        // la corutina para que no siga intentando escribir tras destruir la
        // instancia (causa típica de NullRef si recargas escena en editor).
        _autoFlushRunning = false;
    }
}