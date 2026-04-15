using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class FirebaseSerializer : ISerializer
{
    public string Serialize(List<TrackerEvent> events, bool isFirstBatch)
    {
        StringBuilder sb = new StringBuilder();

        // En Firebase, CADA envío es un array independiente. No hay comas previas.
        sb.AppendLine("[");

        for (int i = 0; i < events.Count; i++)
        {
            string jsonEvent = JsonUtility.ToJson(events[i]);
            sb.Append("    ").Append(jsonEvent);

            if (i < events.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }

        sb.AppendLine("]");
        return sb.ToString();
    }

    // Firebase no usa cabeceras ni pies de página en formato texto
    public string GetHeader() { return ""; }
    public string GetFooter() { return ""; }
}