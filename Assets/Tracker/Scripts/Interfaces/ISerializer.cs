using System.Collections.Generic; 

public interface ISerializer
{
    /// <summary>
    /// Serializa la lista de eventos a un formato específico (por ejemplo, JSON, XML, CSV...)
    /// </summary>
    /// <param name="events">Lista de eventos a serializar</param>
    /// <returns>La representación serializada de la lista de eventos como una cadena</returns>
    string Serialize(List<TrackerEvent> events, bool isFirstBatch);
    string GetHeader();
    string GetFooter(); 
}
