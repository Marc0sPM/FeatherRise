using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Serializador CSV. Genera una fila por evento con el siguiente formato:
///
///   timestamp,session_id,event_type,event_data
///
/// Donde `event_data` es un JSON con SOLO los campos específicos del evento
/// </summary>
public class CSVSerializer : ISerializer
{
    public string Serialize(List<TrackerEvent> events, bool isFirstBatch)
    {
        StringBuilder sb = new StringBuilder();

        foreach (TrackerEvent e in events)
        {
            string baseCols = $"{e.timestamp},{e.session_id},{e.event_type},";

            // Sacamos solo los campos ESPECÍFICOS del evento (no los de la clase base)
            string specificJson = SerializeSpecificFields(e);

            // Escapar comillas dobles duplicándolas (regla CSV estándar)
            string escaped = specificJson.Replace("\"", "\"\"");

            sb.Append(baseCols).Append('"').Append(escaped).Append('"').AppendLine();
        }

        return sb.ToString();
    }

    public string GetHeader() => "timestamp,session_id,event_type,event_data\n";
    public string GetFooter() => "";

    /// <summary>
    /// Serializa solo los campos declarados en la subclase concreta del evento,
    /// ignorando los heredados de TrackerEvent (timestamp, session_id, event_type).
    /// Usa reflexión una vez por tipo y cachea el resultado implícitamente a través
    /// del JsonUtility.
    /// </summary>
    private static string SerializeSpecificFields(TrackerEvent e)
    {
        // Campos públicos declarados SOLO en la clase hija (no heredados)
        FieldInfo[] ownFields = e.GetType().GetFields(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        if (ownFields.Length == 0)
        {
            // Eventos sin payload (Session_Start, Session_End) -> JSON vacío
            return "{}";
        }

        StringBuilder sb = new StringBuilder();
        sb.Append('{');
        for (int i = 0; i < ownFields.Length; i++)
        {
            FieldInfo f = ownFields[i];
            object v = f.GetValue(e);

            sb.Append('"').Append(f.Name).Append("\":");
            AppendJsonValue(sb, v);

            if (i < ownFields.Length - 1) sb.Append(',');
        }
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Serializa un valor simple a JSON. Cubre los tipos que realmente
    /// aparecen en nuestros eventos (bool, int, long, float, string, enum).
    /// Si aparece un tipo complejo se delega a JsonUtility.ToJson.
    /// </summary>
    private static void AppendJsonValue(StringBuilder sb, object v)
    {
        if (v == null) { sb.Append("null"); return; }

        switch (v)
        {
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case string s:
                sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
                break;
            case float f:
                sb.Append(f.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            case double d:
                sb.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            case int i:
                sb.Append(i);
                break;
            case long l:
                sb.Append(l);
                break;
            case System.Enum:
                sb.Append('"').Append(v.ToString()).Append('"');
                break;
            default:
                // Fallback genérico (estructuras de Unity, clases anidadas, etc.)
                sb.Append(JsonUtility.ToJson(v));
                break;
        }
    }
}