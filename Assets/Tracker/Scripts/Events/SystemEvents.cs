using UnityEngine;

// ------------------------------
//    EVENTOS DE SESION
// ------------------------------

[System.Serializable]
public class Session_Start : TrackerEvent
{
    public Session_Start() : base() { }
}

[System.Serializable]
public class Session_End : TrackerEvent
{
    public Session_End() : base() { }
}

// ------------------------------
//    EVENTOS DE SESION
// ------------------------------

[System.Serializable]
public class Level_Start : TrackerEvent
{
    public int level_id;

    public Level_Start(int level_id) : base()
    {
        this.level_id = level_id;
    }
}

public enum LevelResult
{
    Completed, 
    Quit
}

[System.Serializable]
public class Level_End : TrackerEvent
{
    public int level_id;
    public string result; 

    public Level_End(int level_id, LevelResult result) : base()
    {
        this.level_id = level_id;
        this.result = result.ToString().ToLower();
    }
}