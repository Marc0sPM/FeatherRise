using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class InputComponent : MonoBehaviour
{
    private float _playerHorizontalDirection;
    private Vector2 _mousePosition;
    private MovementComponent _myMovementComponent;
    private FeatherThrowComponent _myFeatherThrowComponent;
    private PlayerCombat _myAttackComponent;
    private ChestComponent _myChestComponent;
    [SerializeField] private UIManager _myUIManager;
    [SerializeField]
    private float _dashCoolDown;
    [SerializeField] private Slider _slide;
    private float _timer;
    private bool _oncd;

    [Header("ATTACK")]
    private float _timeToDash = 0;
    [SerializeField]
    private float _attackRate;
    [SerializeField]
    private float _attackRateOnAir;

    float _nextAttackTime = 0f;
    [SerializeField] private LayerMask _interactuableLayer;
    
    // Start is called before the first frame update
    void Start()
    {
        
        //_timer = 0;
        _oncd = false;
        _myMovementComponent = GetComponent<MovementComponent>();
        _myFeatherThrowComponent = GetComponent<FeatherThrowComponent>();
        _myAttackComponent = GetComponent<PlayerCombat>();
    }

    // Update is called once per frame
    void Update()
    {
        _playerHorizontalDirection = Input.GetAxisRaw("Horizontal");       // Se recibe Input Horizontal
        if (_playerHorizontalDirection != 0)                            // Si hay movimiento horizontal
        {
            _myMovementComponent.Move(_playerHorizontalDirection);      // Llama al método Move (Horizontal) del MovementComponent
        }

        else
        {
            _myMovementComponent.Move(0);
        }

        if (Input.GetButtonDown("Jump"))                                // Si se recibe Input de Salto
        {
            _myMovementComponent.Jump();                                // Llama al método Jump del MovementComponent
        }

        
        
        if (Input.GetButtonDown("Dash") && _timeToDash > _dashCoolDown && !_myMovementComponent.TouchingFloor) // Si se recibe Input de Dash 
        {
            _oncd= true;
            _slide.maxValue = _dashCoolDown;
            _myMovementComponent.StartCoroutine(_myMovementComponent.Dash());  // Llama al método Dash del MovementComponent

            if (!GetComponent<MovementComponent>().CanDash)
            {
                _timeToDash = 0;
            }

        }
        if (_oncd)
        {
            _timer += Time.deltaTime;
            if (_timer < _dashCoolDown)
            {
                _slide.value = _timer;
            }
            else if (_timer >= _dashCoolDown)
            {
                _oncd = false;
                _timer = 0;
            }
        }
        

        if (Input.GetButtonDown("FeatherThrow"))                        // Si se recibe Input de Lanzamiento de Pluma
        {
            _mousePosition = Input.mousePosition;                       // Se guarda la posición del cursor

            //si dirección player y destino flechas son contrarios girar player
            
            
            _myFeatherThrowComponent.FeatherObjective(_mousePosition);
            if (GameManager.Instance.FeatherCant > 0)
            {
                GameManager.Instance.RemoveFeather();
            }
            
            
        }

        if (Time.time >= _nextAttackTime && GetComponent<MovementComponent>().TouchingFloor)
        {
            if (Input.GetButtonDown("Basic Attack") && GameManager.Instance._sword == true)
            {
                Debug.Log("En el suelo");
                _myAttackComponent.Attack();
                _nextAttackTime = Time.time + 1f / _attackRate; //de esta forma decidimos las veces que podemos atacar por segundo
                _myMovementComponent.Move(0);
            }
        }
        if (Time.time >= _nextAttackTime && !GetComponent<MovementComponent>().TouchingFloor)
        {
            if (Input.GetButtonDown("Basic Attack") && GameManager.Instance._sword == true)
            {
                Debug.Log("En el aire");
                _myAttackComponent.Attack();
                _nextAttackTime = Time.time + 1f / _attackRateOnAir; //de esta forma decidimos las veces que podemos atacar por segundo
                _myMovementComponent.Move(0);
            }
        }


        if (Input.GetButtonDown("Interact"))
        {
            //Habría q ver la interacción del cofre desde input, de momento en el propio cofre
        }
        if(_timeToDash <= _dashCoolDown)
        {
            _timeToDash += Time.deltaTime;
        }
        if (Input.GetButtonDown("Cancel"))
        {
            _myUIManager.Pause();
        }
       

    }
}


