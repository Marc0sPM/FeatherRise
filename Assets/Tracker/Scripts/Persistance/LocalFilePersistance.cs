using System.IO;
using UnityEngine;

public class LocalFilePersistence : IPersistence
{
    private string filePath;

    public LocalFilePersistence(string sessionId, string fileExtension)
    {
        string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"telemetry_{sessionId}_{date}" + fileExtension;
        filePath = Path.Combine(Application.persistentDataPath, fileName);
    }


    public void Save(string data)
    {
        File.AppendAllText(filePath, data);
    }

    public void Close(string footer)
    {
        File.AppendAllText(filePath, footer); 
        Debug.Log("[Tracker] Archivo cerrado en disco.");
    }
    public void Open(string header)
    {
        File.AppendAllText(filePath, header); 
    }
}