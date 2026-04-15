using System.Net.Http;
using System.Text;
using UnityEngine;

public class FirebasePersistence : IPersistence
{
    private string _databaseUrl;

    // HttpClient es thread-safe y es estatico para reutilizar conexiones
    private static readonly HttpClient _httpClient = new HttpClient();

    public FirebasePersistence(string baseUrl, string sessionId)
    {
        // Limpiamos la URL 
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');

        // Construimos la URL REST de Firebase. Siempre debe terminar en .json
        _databaseUrl = $"{baseUrl}/telemetry/{sessionId}.json";
    }

    public void Open(string header)
    {
        // En una base de datos web no se abren ni escriben cabeceras
    }

    public void Save(string data)
    {
        // Preparamcion del paquete de datos en formato JSON (UTF8)
        var content = new StringContent(data, Encoding.UTF8, "application/json");

        // Como Tracker llama a Save() desde un hilo secundario,
        // se usa .Result para esperar la respuesta de internet sin congelar el juego.
        HttpResponseMessage response = _httpClient.PostAsync(_databaseUrl, content).Result;

        if (!response.IsSuccessStatusCode)
        {
            // Si el jugador se queda sin internet o Firebase se cae, lanzamos una excepción.
            throw new System.Exception($"Error de red: {response.StatusCode} - {response.ReasonPhrase}");
        }
    }

    public void Close(string footer)
    {
        // No hay archivos locales que cerrar
    }
}