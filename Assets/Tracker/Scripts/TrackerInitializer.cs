using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class TrackerInitializer : MonoBehaviour
{
    public enum S_type
    {
        JSON,
        CSV
    };

    public enum P_type
    {
        LOCAL_FILE,
        FIREBASE
    }
    [Header("Ajustes de inicializacion")]
    public S_type serializerType;
    public P_type persistanceType;

    [Header("Ajustes de Servidor")]
    public string firebaseDatabaseUrl = "https://featherrise-telemetry-p3-default-rtdb.europe-west1.firebasedatabase.app/"; 

    // Extension para el archivo de guardado
    private string _fileExtension; 
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
        // Firebase necesita un serializador especifico, porque usa el formato JSON
        // pero distinto al JSON para un archivo local
        if (persistanceType == P_type.FIREBASE)
        {
            return new FirebaseSerializer();
        }

        switch (serializerType) {
            case S_type.CSV:
                _fileExtension = ".csv"; 
                return new CSVSerializer();
            case S_type.JSON:
            default:    // por defecto JSON
                _fileExtension = ".json"; 
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
            case P_type.FIREBASE:
                return new FirebasePersistence(firebaseDatabaseUrl, sessionId);
            case P_type.LOCAL_FILE:
            default: // por defecto archivo local
                return new LocalFilePersistence(sessionId, _fileExtension);
        } 
    } 

}
