using System.IO;
using UnityEngine;

public class LocalFilePersistence : IPersistence
{
    private string filePath;

    public LocalFilePersistence(string sessionId)
    {
        string date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"telemetry_{sessionId}_{date}.json";
        filePath = Path.Combine(Application.persistentDataPath, fileName);
    }


    public void Save(string data)
    {
        File.AppendAllText(filePath, data);
    }

    public void Close()
    {
        Debug.Log("[Tracker] Archivo cerrado en disco.");
    }
}