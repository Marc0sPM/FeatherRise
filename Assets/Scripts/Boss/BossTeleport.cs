using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossTeleport : MonoBehaviour
{
    [SerializeField] private Transform _teleportPoint1;
    [SerializeField] private Transform _teleportPoint2;
    private bool _teleportedOnce = false;

    public void TeleportToPoint()
    {
        SoundComponent.Instance.PlaySound(SoundComponent.Instance._bossTeleport);
        if (!_teleportedOnce)
        {
            transform.position = _teleportPoint1.position;
            _teleportedOnce = !_teleportedOnce;
        }
        else
        {
            transform.position = _teleportPoint2.position;
            _teleportedOnce = !_teleportedOnce;
        }

    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
