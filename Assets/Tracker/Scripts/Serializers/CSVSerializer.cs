using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CSVSerializer : ISerializer
{
    public string Serialize(List<TrackerEvent> events, bool isFirstBatch)
    {
        StringBuilder sb = new StringBuilder();

        foreach (TrackerEvent e in events)
        {
            // datos base 
            string baseData = $"{e.timestamp},{e.session_id},{e.event_type},";

            // datos especificos, pero los datos base duplicados, se limpian de forma externa en el analisis de datos
            string specificData = JsonUtility.ToJson(e);

            specificData = specificData.Replace("\"", "\"\"");

            sb.AppendLine($"{baseData}\"{specificData}\"");
        }

        return sb.ToString();
    }

    public string GetHeader()
    {
        return "timestamp,session_id,event_type,event_data\n";
    }

    public string GetFooter()
    {
        return "";
    }
}