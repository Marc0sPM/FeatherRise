using System;

[System.Serializable]
public abstract class TrackerEvent
{
    public long timestamp;
    public string session_id;
    public string event_type;

    public TrackerEvent()
    {
        // Se autocompleta con el nombre exacto de la clase hija que lo llame
        // Por ejemplo, si creas un "Player_Death", event_type sera "Player_Death"
        this.event_type = this.GetType().Name; 
    }
}