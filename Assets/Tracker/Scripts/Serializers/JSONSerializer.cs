using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class JSONSerializer : ISerializer
{
    public string Serialize(List<TrackerEvent> events)
    {
        StringBuilder sb = new StringBuilder();
        
        // Abrimos el array JSON
        sb.AppendLine("["); 

        for (int i = 0; i < events.Count; i++)
        {
            // JsonUtility sí funciona bien si le pasamos el objeto individual
            string jsonEvent = JsonUtility.ToJson(events[i]);
            sb.Append("  ").Append(jsonEvent);

            // Añadimos una coma si no es el último elemento
            if (i < events.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        // Cerramos el array JSON
        sb.AppendLine("]");

        return sb.ToString();
    }
}
