using System.Collections.Generic;
using System.Text;

/// <summary>
/// Serializador a JSON. Produce un archivo con una lista de listas:
///   [
///     [ {evento1}, {evento2}, ... ],   ← primer flush
///     [ {eventoN}, {eventoN+1}, ... ], ← segundo flush
///     ...
///   ]
/// </summary>
public class JSONSerializer : ISerializer
{
    public string Serialize(List<TrackerEvent> events, bool isFirstBatch)
    {
        StringBuilder sb = new StringBuilder();

        // Separador con el batch anterior. La coma solo va si NO es el primero.
        if (!isFirstBatch)
        {
            sb.AppendLine(",");
        }

        sb.Append("  [");
        sb.AppendLine();

        for (int i = 0; i < events.Count; i++)
        {
            string jsonEvent = UnityEngine.JsonUtility.ToJson(events[i]);
            sb.Append("    ").Append(jsonEvent);

            if (i < events.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }

        sb.Append("  ]");
        return sb.ToString();
    }

    public string GetHeader() => "[\n";
    public string GetFooter() => "\n]";
}