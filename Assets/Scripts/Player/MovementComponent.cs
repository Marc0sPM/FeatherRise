using System.Collections;
using UnityEngine;



public class MovementComponent : MonoBehaviour
{
    private Rigidbody2D _playerRB;
    private Transform _playerTransform;

    [SerializeField] private float _rayDistance;
    [SerializeField] private float _offSet;

    [Header("Jump")]

    [SerializeField] private Transform _floorControler; //controlador del suelo
    [SerializeField] private Vector2 _floorDimensions;
    [SerializeField] private LayerMask _floor;
    [SerializeField] private float _jumpForce;

    [Header("Movement")]

    [SerializeField] private Transform _wallsController;
    [SerializeField] private Vector2 _wallsDimensions;
    [SerializeField] private LayerMask _walls;
    [SerializeField] private float _moveSpeed;


    private bool _touchingWalls;

    private bool _touchingFloor;
    public bool TouchingFloor { get { return _touchingFloor; } }
    private bool _lookingRight = true;
    public bool LookingRight { get { return _lookingRight; } }
    

    [Header("Dash")]

    [SerializeField] private float _dashVelocity;
    [SerializeField] private float _timeDash;
    [SerializeField] private TrailRenderer _trail;
    


    private float _initialGravity;
    private bool _canDash = true;
    public bool CanDash { get { return _canDash; } }

    // private bool _canMove = true; (no se usa)

    private Animator _playerAnim;


    // Start is called before the first frame update
    void Start()
    {
        _playerRB = GetComponent<Rigidbody2D>();
        _playerAnim = GetComponent<Animator>();
        _playerTransform = transform;
        _initialGravity = _playerRB.gravityScale;

    }
    private void Update()
    {
        _playerAnim.SetBool("onFloor", _touchingFloor);
        _playerAnim.SetFloat("Horizontal", Mathf.Abs(_playerRB.velocity.x));
        _playerAnim.SetFloat("Vertical", _playerRB.velocity.y);
        _playerAnim.SetBool("Dash", _canDash);
    }
    //evaluacion de las fisicas para saber si toca suelo o una pared
    private void FixedUpdate()
    {
        _touchingFloor = Physics2D.OverlapBox(_floorControler.position, _floorDimensions, 0, _floor);
        _touchingWalls = Physics2D.OverlapBox(_wallsController.position, _wallsDimensions, 0, _walls);
    }
    //metodo para saltar dandole una fuerza de impulso al player
    public void Jump()
    {
        if (_touchingFloor)
        {
            SoundComponent.Instance.PlaySound(SoundComponent.Instance._jump);
            _playerRB.AddForce(new Vector2(0, _jumpForce), ForceMode2D.Impulse);
        }

    }
    //metodo que comprueba si toca una pared y si no, mueve al jugador en funcion de un valor (desde -1 a 1)
    public void Move(float _playerDirection)
    {
        if(!_touchingWalls)
        {
            _playerRB.velocity = new Vector2(_playerDirection * _moveSpeed, _playerRB.velocity.y);
        }
        //evaluaciones auxiliares para llamar al metodo turn
        if (_playerDirection > 0 && !_lookingRight)
        {
            Turn();
        }
        else if (_playerDirection < 0 && _lookingRight)
        {
            Turn();
        }

    }

    //Metodo de dash 
    public IEnumerator Dash()
    {
       // if (!_touchingFloor) // Dash only if jumping
       // {
            SoundComponent.Instance.PlaySound(SoundComponent.Instance._dash);
            GetComponent<InputComponent>().enabled = false;
            //_canMove = false;
            _canDash = false;
            _trail.emitting = true;
            _playerRB.gravityScale = 0;
            _playerRB.velocity = new Vector2(_dashVelocity * transform.localScale.x, 0);

            yield return new WaitForSeconds(_timeDash); //dash execution time

            _playerRB.velocity = new Vector2(0, _playerRB.velocity.y); //stop dash 
                                                                       //_canMove = true;
            _canDash = true;
            _trail.emitting = false;
            _playerRB.gravityScale = _initialGravity;
            GetComponent<InputComponent>().enabled = true;
       // }
    }
    //metodo que le da la vuelta al jugador en funcion de donde se mueva
    public void Turn()
    {
        _lookingRight = !_lookingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    //metodo auxiliar para ver las cajas de colision del player desde la escena
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_floorControler.position, _floorDimensions);
        Gizmos.DrawWireCube(_wallsController.position, _wallsDimensions);
    }
}


