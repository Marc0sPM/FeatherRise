using System.Resources;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class ThownEnemyComponent : MonoBehaviour
{

    [SerializeField] private float _movSpeed;

    [SerializeField] private int _wallLayerIndex;
    [SerializeField] private TrailRenderer _trailRenderer;

    private bool _dieByWall = false;

    private float _direction = 1;
    private bool _canMove = false;

    private Rigidbody2D _rb2D;
    private Animator _animator;

    [SerializeField] private ParticleSystem _parSys;
    

    private void Start()
    {
        _rb2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.gameObject.layer == _wallLayerIndex || (bool)collision.collider.GetComponent<InputComponent>())
        {
            _rb2D.velocity = Vector2.zero;
            _trailRenderer.emitting = false;
            GetComponent<EnemyComponent>().enabled= false;
            _dieByWall = true;
            _canMove = false;
        }
    }
    public void UnableMove()
    {
        _canMove = !_canMove;
    }
    private void Update()
    {
        _animator.SetBool("Death", _dieByWall);
        if (_canMove) Move();
        
        Debug.Log("speed" + _rb2D.velocity);
    }
    private void Move()
    {
        _rb2D.velocity = new Vector2(_movSpeed * _direction, _rb2D.velocity.y);
    }

    public void SetSpin(float otherScale)
    {
        //Get direction (if boss looking right, then direction = 1, so movement to right; else direction = -1, so move to left)
        if (otherScale < 0) _direction = -1;
        Vector2 scale = transform.localScale;
        scale.x *= _direction;
        transform.localScale = scale;
        _parSys.Play();
        _trailRenderer.emitting = true;
        _canMove = true;
    }
    public void Die()
    {
        
        Destroy(transform.parent.gameObject);
    }

}