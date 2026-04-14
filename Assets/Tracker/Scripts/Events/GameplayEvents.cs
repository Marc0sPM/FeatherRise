using UnityEngine;

// --------------------------------------------------------
// EVENTOS DE JUGABILIDAD (FEATHER RISE)
// --------------------------------------------------------

/// <summary>
/// M4.2 y M4.3 - Registra cuando el jugador alcanza un checkpoint. 
/// Vital para calcular el tiempo entre puntos y asociar muertes a tramos concretos.
/// </summary>
[System.Serializable]
public class Checkpoint_Reached : TrackerEvent
{
    public string checkpoint_id;
    public string level_id;

    public Checkpoint_Reached(string checkpoint_id, string level_id) : base()
    {
        this.checkpoint_id = checkpoint_id;
        this.level_id = level_id;
    }
}

/// <summary>
/// M4.3 - Registra la muerte del jugador.
/// Se guardan las coordenadas para hacer mapas de calor y la causa para saber qué los mata.
/// </summary>
[System.Serializable]
public class Player_Death : TrackerEvent
{
    public float pos_x;
    public float pos_y;
    public string cause_of_death;
    public string level_id;

    public Player_Death(float x, float y, string cause, string level) : base()
    {
        this.pos_x = x;
        this.pos_y = y;
        this.cause_of_death = cause;
        this.level_id = level;
    }
}

/// <summary>
/// Hipótesis 1 - Gestión de recursos y fricción de controles.
/// Evalúa si el jugador intenta recoger plumas y falla (ej. pulsar 'W' por error al saltar).
/// </summary>
[System.Serializable]
public class Feather_Recall_Attempt : TrackerEvent
{
    public bool is_successful;

    public Feather_Recall_Attempt(bool is_successful) : base()
    {
        this.is_successful = is_successful;
    }
}

/// <summary>
/// M5.1 - Interacción con cofres. 
/// Esencial para cruzar session_id únicos que han abierto el cofre del Nivel 2.2.
/// </summary>
[System.Serializable]
public class Chest_Opened : TrackerEvent
{
    public string chest_id;
    public string level_id;

    public Chest_Opened(string chest_id, string level_id) : base()
    {
        this.chest_id = chest_id;
        this.level_id = level_id;
    }
}

// Enum de apoyo para el tipo de ataque
public enum AttackType
{
    Ground,
    Aerial
}

/// <summary>
/// M7.1 y M7.2 - Ratio de uso de ataques y Hit Rate (Precisión).
/// </summary>
[System.Serializable]
public class Player_Attack : TrackerEvent
{
    public string attack_type;
    public bool enemy_hit;

    public Player_Attack(AttackType attackType, bool enemyHit) : base()
    {
        // Convertimos el enum a string para que en el JSON ponga "Ground" o "Aerial" y no 0 o 1
        this.attack_type = attackType.ToString();
        this.enemy_hit = enemyHit;
    }
}