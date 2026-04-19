using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

public class Tracker : MonoBehaviour
{
    // Instancia singleton del Tracker
    public static Tracker Instance { get; private set; }
    
    // Dependencias para serialización y persistencia de datos
    private ISerializer _serializer;
    private IPersistence _persistence;
    private bool _isFirstFlush = true; 

    // Cola de eventos a ser procesados
    private ConcurrentQueue<TrackerEvent> _eventQueue = new ConcurrentQueue<TrackerEvent>();   
    // Identificador único para cada sesión de juego
    private string _currentSessionId; 

    [Header("Configuración del Tracker")]
    [Tooltip("Tiempo en segundos para hacer guardado automatico de datos")]
    public float autoFlushInterval = 30f;

    private void Awake()
    {
        // Singleton pattern
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    /// <summary>
    /// Inicializa el Tracker con las dependencias necesarias para su funcionamiento.
    /// </summary>
    /// <param name="ser">El serializador a utilizar</param>
    /// <param name="pers">La implementación de persistencia a utilizar</param>
    /// <param name="sessionId">Identificador unico de sesion</param>
    public void Init(ISerializer ser, IPersistence pers, string sessionId)
    {
        _serializer = ser;
        _persistence = pers;

        _currentSessionId = sessionId; // Genera un ID unico para la sesion actual

        Debug.Log($"[TRACKER] Inicializado. Sesion ID: {_currentSessionId}");

        _persistence.Open(_serializer.GetHeader()); 
        TrackEvent(new Session_Start()); // Trackeamos evento de inicio de sesion
        StartCoroutine(AutoFlushCoroutine());
    }

    /// <summary>
    /// Agrega un evento a la cola de eventos del Tracker. 
    /// El evento se completará con datos fijos como timestamp y session_id antes de ser añadido a la cola.
    /// </summary>
    /// <param name="e">Evento que se añade</param>
    public void TrackEvent(TrackerEvent e)
    {
        // Datos fijos
        e.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        e.session_id = _currentSessionId;

        _eventQueue.Enqueue(e);
    }

    /// <summary>
    /// Procesa la cola de eventos, serializa los datos y los guarda utilizando las dependencias configuradas.
    /// </summary>
    public void Flush(bool forceSynchronous = false)
    {
        if(_eventQueue.IsEmpty) return; 

        // Vaciamos cola a una lista
        List<TrackerEvent> eventsToFlush = new List<TrackerEvent>();
        while(_eventQueue.TryDequeue(out TrackerEvent e))
        {
            eventsToFlush.Add(e);
        }

        if (forceSynchronous) 
        {
            serializeAndSave(eventsToFlush);
        }
        else
        {
            // Lanzamos hilo secundario
            Task.Run(() =>
            {
                serializeAndSave(eventsToFlush); 
            });
        }
    }

    private void serializeAndSave(List<TrackerEvent> eventsToFlush)
    {
        try
        {
            string data = _serializer.Serialize(eventsToFlush, _isFirstFlush);
            _persistence.Save(data); 
            if(_isFirstFlush) _isFirstFlush = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TRACKER] Error al guardar datos: {ex.Message}");
            ReturnEventsToQueue(eventsToFlush);
        }
    }

    /// <summary>
    /// Devuelve a la cola los eventos de la lista
    /// </summary>
    private void ReturnEventsToQueue(List<TrackerEvent> failedEvents)
    {
        foreach (var e in failedEvents)
        {
            _eventQueue.Enqueue(e);
        }
    }

    /// <summary>
    /// Corutina que se encarga de guardar cada autoFlushInterval segundos en segundo plano
    /// </summary>
    /// <returns></returns>
    private IEnumerator AutoFlushCoroutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(autoFlushInterval);
            Flush(false);
        }
    }
   
    private void OnApplicationQuit()
    {
        TrackEvent(new Session_End()); // Trackeamos evento de fin de sesion
        // Vaciamos por ultima vez , forzamos a que sea sincrono
        Flush(true);
        if(_persistence != null)
        {
            // Cerramos conexiones o streams si es necesario
            _persistence.Close(_serializer.GetFooter());
        }
        
    }
}
