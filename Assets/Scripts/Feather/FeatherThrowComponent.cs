using Cinemachine;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class FeatherThrowComponent : MonoBehaviour
{
    private Vector2 _relativeFeatherPosition;
    private Camera _myCam;
    private MovementComponent _movementComp;
    private Vector2 _playerScreenPosition;
    private GameObject _player;
    //private float _featherAngle;
    //Instancia publica del propio player para pasar posicion constante para el Return de la pluma
    [SerializeField] private GameObject _featherPrefab;
    public Transform spawnPoint;

    // Start is called before the first frame update
    void Start()
    {
        _myCam = Camera.main;
        _movementComp = GetComponent<MovementComponent>();
        _player = GameManager.Instance.SetPlayer();
    }


    //Calcula direccion de lanzaminto e instancia la pluma 
    public void FeatherObjective(Vector2 mousePosition)                   // Se calcula la posición donde se lanzará la pluma
    {
        
        _playerScreenPosition = _myCam.WorldToScreenPoint(gameObject.transform.position);   // Posición del jugador en la pantalla

        _relativeFeatherPosition = mousePosition - _playerScreenPosition;   // Posición del ratón respecto al jugador
        Debug.Log(_relativeFeatherPosition);
    
        if(_relativeFeatherPosition.x < 0 && _movementComp.LookingRight)
        {
            _movementComp.Turn();
            
        }
        else if(_relativeFeatherPosition.x > 0 && !_movementComp.LookingRight)
        {
            _movementComp.Turn();
        }
        if (GameManager.Instance.FeatherCant > 0)
        {
            GameObject go = Instantiate(_featherPrefab, spawnPoint.position, Quaternion.identity);
        }
            
        
    }
    
    

    
}