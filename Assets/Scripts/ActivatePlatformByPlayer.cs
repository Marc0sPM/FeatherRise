using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ActivatePlatformByPlayer : MonoBehaviour
{
    [SerializeField] private PlatformComponent _myPlatformComponent;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((bool)collision.gameObject.GetComponent<InputComponent>())
        {
            _myPlatformComponent.ChangeMove();
        }
    }
}
