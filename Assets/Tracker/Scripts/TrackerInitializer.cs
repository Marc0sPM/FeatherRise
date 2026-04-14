using UnityEditor;
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

        IPersistence mPersistence = choosePersistence(); 

        Tracker.Instance.Init(mSerializer, mPersistence);

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

    private IPersistence choosePersistence() 
    {
        string tempSessionId = System.Guid.NewGuid().ToString();
        switch (persistanceType) {
            case P_type.LOCAL_FILE:
            default: // por defecto archivo local
                return new LocalFilePersistence(tempSessionId);
        } 
    } 

}
