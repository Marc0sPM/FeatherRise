using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetectorComponent : MonoBehaviour
{
    private FallingPlatformComponent _myFallingPlatformComponent;

    private void Start()
    {
        _myFallingPlatformComponent = GetComponent<FallingPlatformComponent>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((bool)collision.gameObject.GetComponent<InputComponent>())
        {
            Debug.Log("Subido en la plataforma");
            _myFallingPlatformComponent.StartCoroutine(_myFallingPlatformComponent.Falling());
        }
    }
}
