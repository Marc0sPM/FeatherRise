
using UnityEngine;

public class PatrolComponent : MonoBehaviour
{
    Rigidbody2D _enemyRB;
    Transform _enemyTransform;
    Animator _animator;
    private bool _lookingRight;
    public bool lookingRight { get { return _lookingRight; } }
    //private bool _touchingFloor;
    private bool _stop;

    [Header("Move")]

    [SerializeField] private LayerMask _patrolStop;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private Transform _detectorControler;
    [SerializeField] private Vector2 _detectorDimensions;
    [SerializeField] private float _timeCoolDown;

    private float _time;

    private float _direction = 1;
    public float Direction { get { return _direction; } }

    

    // Start is called before the first frame update
    void Start()
    {
        _enemyRB = GetComponent<Rigidbody2D>();
        _enemyTransform = transform;
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        if(_stop)
        {
            _enemyRB.velocity = new Vector2(0, 0);
            if(_time > _timeCoolDown)
            {
                Turn();
                Move();
                _time = 0;
            }
            _time += Time.deltaTime;
        }
        _animator.SetFloat("Horizontal", Mathf.Abs(_enemyRB.velocity.x));
    }
    private void FixedUpdate()
    {
        _stop = Physics2D.OverlapBox(_detectorControler.position, _detectorDimensions, 0, _patrolStop);
    }
    private void Move()
    {
        _enemyRB.velocity = new Vector2(_moveSpeed * _direction * -1, _enemyRB.velocity.y);
        
    }

    public void Turn()
    {
        _lookingRight = !_lookingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        _direction *= -1;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_detectorControler.position, _detectorDimensions);
    }
}
