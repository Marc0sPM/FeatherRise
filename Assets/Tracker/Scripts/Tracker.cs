using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tracker : MonoBehaviour
{
    // Instancia singleton del Tracker
    public static Tracker Instance { get; private set; }
    
    // Dependencias para serialización y persistencia de datos
    private ISerializer _serializer;
    private IPersistence _persistence;

    // Cola de eventos a ser procesados
    private Queue<TrackerEvent> _eventQueue = new Queue<TrackerEvent>();   
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
    public void Init(ISerializer ser, IPersistence pers)
    {
        _serializer = ser;
        _persistence = pers;

        _currentSessionId = System.Guid.NewGuid().ToString(); // Genera un ID unico para la sesion actual

        Debug.Log($"[TRACKER] Inicializado. Sesion ID: {_currentSessionId}");

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
    public void Flush()
    {
        if(_eventQueue.Count == 0) return; 

        // Vaciamos cola a una lista
        List<TrackerEvent> eventsToFlush = new List<TrackerEvent>();
        while(_eventQueue.Count > 0)
        {
            eventsToFlush.Add(_eventQueue.Dequeue());
        }

        try
        {
            // Serializamos y guardamos los datos
            string data = _serializer.Serialize(eventsToFlush);
            _persistence.Save(data);
        } 
        catch (System.Exception ex)
        {
            Debug.LogError($"[TRACKER] Error al guardar datos: {ex.Message}");
            foreach(var e in eventsToFlush)
            {
                // Si falla el guardado devolvemos a la cola
                _eventQueue.Enqueue(e);
            }
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
            Flush();
        }
    }
   
    private void OnApplicationQuit()
    {
        // Vaciamos por ultima vez 
        Flush();
        if(_persistence != null)
        {
            // Cerramos conexiones o streams si es necesario
            _persistence.Close();
        }
        
    }
}
