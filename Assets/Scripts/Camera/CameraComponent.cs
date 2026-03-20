using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraComponent : MonoBehaviour
{
    private Camera _myCameraComponent;
    private Vector2 _mousePosition;

    // Start is called before the first frame update
    void Start()
    {
        _myCameraComponent = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
