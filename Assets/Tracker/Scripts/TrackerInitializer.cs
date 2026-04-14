using UnityEngine;

public class TrackerInitializer : MonoBehaviour
{
    public enum S_type
    {
        JSON
    };

    public enum P_type
    {
        LOCAL_FILE
    }
    [Header("Ajustes de inicializacion")]
    public S_type serializerType;
    public P_type persistanceType;
    void Start()
    {
        // Inicilizamos el tracker con el serializador y el sistema de persistencia deseados.
        ISerializer mSerializer = chooseSerializer();

        string sessionId = System.Guid.NewGuid().ToString();
        IPersistence mPersistence = choosePersistence(sessionId); 

        Tracker.Instance.Init(mSerializer, mPersistence, sessionId);

    }

    /// <summary>
    /// Crea el serializador deseado a partir del tipo elegido
    /// </summary>
    private ISerializer chooseSerializer()
    {

        switch (serializerType) {
            case S_type.JSON:
            default:    // por defecto JSON, cuando haya mas tendra sentido
                return new JSONSerializer();
        }
    }

    /// <summary>
    /// Crea la clase de persitencia deaseada a partir de P_type
    /// </summary>
    /// <param name="sessionId">Identidifador unico de sesion</param>
    private IPersistence choosePersistence(string sessionId) 
    {
        switch (persistanceType) {
            case P_type.LOCAL_FILE:
            default: // por defecto archivo local
                return new LocalFilePersistence(sessionId);
        } 
    } 

}
