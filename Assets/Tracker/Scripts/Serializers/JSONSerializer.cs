using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class JSONSerializer : ISerializer
{
    public string Serialize(List<TrackerEvent> events, bool isFirstBatch)
    {
        StringBuilder sb = new StringBuilder();

        // Si no es el primer volcado, separamos el lote anterior de este con una coma
        if (!isFirstBatch)
        {
            sb.AppendLine(",");
        }

        sb.AppendLine("  ["); // Abre el array de este lote específico

        for (int i = 0; i < events.Count; i++)
        {
            string jsonEvent = JsonUtility.ToJson(events[i]);
            sb.Append("    ").Append(jsonEvent);

            if (i < events.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }

        sb.Append("  ]"); // Cierra el array de este lote

        return sb.ToString();
    }

    public string GetHeader()
    {
        return "[\n"; 
    }

    public string GetFooter()
    {
        return "\n]"; 
    }
}