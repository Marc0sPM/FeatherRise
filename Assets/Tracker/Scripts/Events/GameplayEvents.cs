using UnityEngine;

// --------------------------------------------------------
// EVENTOS DE JUGABILIDAD
// --------------------------------------------------------

/// <summary>
/// M4.2 y M4.3 - Registra cuando el jugador alcanza un checkpoint.
/// </summary>
[System.Serializable]
public class Checkpoint_Reached : TrackerEvent
{
    public float pos_x;
    public float pos_y;

    public Checkpoint_Reached(float pos_x, float pos_y) : base()
    {
        this.pos_x = pos_x;
        this.pos_y = pos_y;
    }
}

/// <summary>
/// M4.3 - Registra la muerte del jugador.
/// </summary>
[System.Serializable]
public class Player_Death : TrackerEvent
{
    public float pos_x;
    public float pos_y;
    public string cause_of_death;

    public Player_Death(float x, float y, string cause) : base()
    {
        this.pos_x = x;
        this.pos_y = y;
        this.cause_of_death = cause;
    }
}

/// <summary>
/// Hipótesis 1 - Gestión de recursos y fricción de controles.
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
/// </summary>
[System.Serializable]
public class Chest_Opened : TrackerEvent
{
    public string chest_id;
    public int level_id;

    public Chest_Opened(string chest_id, int level_id) : base()
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
        this.attack_type = attackType.ToString().ToLower();
        this.enemy_hit = enemyHit;
    }
}