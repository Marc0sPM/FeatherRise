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
