using System.IO;
using UnityEngine;

public class LocalFilePersistence : IPersistence
{
    private string _filePath;

    public LocalFilePersistence(string sessionId)
    {
        // Creamos el nombre del archivo usando la sesión y la fecha de hoy
        string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"telemetry_{sessionId}_{date}.json";
        
        // Combinamos la ruta segura de Unity con el nombre del archivo
        _filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        Debug.Log($"[Tracker] Los datos se guardarán en: {_filePath}");
    }

    /// <summary>
    /// Persiste datos en disco añadiendo al archivo, manteniendo formato JSON Lines para mejor análisis de telemetría.
    /// Usa AppendAllText para evitar perder datos de sesiones anteriores y permitir registro secuencial de eventos sin reestructurar arrays.
    /// Lanza IOException si la operación de escritura falla, permitiendo que la clase llamante (Tracker) maneje el error y reencole eventos.
    /// </summary>
    public void Save(string data)
    {
        try
        {   
            // Añadir al final del archivo (modo JSON Lines es mejor para telemetría)
            File.AppendAllText(_filePath, data);
        }
        catch (IOException e)
        {
            Debug.LogError($"[Tracker] Error de escritura en disco: {e.Message}");
            throw;
        }
    }

    public void Close()
    {
        // En escritura local simple no hace falta cerrar streams abiertos constantemente. 
        Debug.Log("[Tracker] Archivo de persistencia cerrado.");
    }
}