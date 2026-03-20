using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerComponent : MonoBehaviour
{
    private Vector3 _myCursorPosition;
    private float _horizontal;
    private float _vertical;
    [SerializeField] private float _sensitivity;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _horizontal = Input.GetAxis("Joystick Right X");
        _vertical = Input.GetAxis("Joystick Right Y");
        _myCursorPosition = Input.mousePosition;
        
        _myCursorPosition.x += _horizontal * _sensitivity;
        _myCursorPosition.y += _vertical * _sensitivity;

        _myCursorPosition.x = Mathf.Clamp(_myCursorPosition.x, 0, Screen.width);
        _myCursorPosition.y = Mathf.Clamp(_myCursorPosition.y, 0, Screen.height);
            
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined; //hace que el cursor no pueda salir de la ventana de juego
        transform.position = _myCursorPosition;
        
        

    }
}
