using System.Collections;

using UnityEngine;

public class SpinComponent : MonoBehaviour
{
    private Rigidbody2D _enemyRB;
    private GameObject _player;
    private PatrolComponent _patrol;
    private Animator _animator;
    Vector2 _inicialVelocity;

    [SerializeField] private ParticleSystem _parSys;

    [Header("Spin")]

    [SerializeField] private float _spinVelocity;
    [SerializeField] private float _timeCharge;
    [SerializeField] private float _timeSpin;
    [SerializeField] private float _spinCoolDown = 4;
    private float _currentTime = 0;
    private bool _finishCharge = true;
    private bool _enabledSpinDamage =  false; //allows to activate 2 point life out
    public bool EnableSpinDamage { get { return _enabledSpinDamage; } }

    private bool _touchingFloor;
    public bool TouchingFloor { get { return _touchingFloor; } }

    private bool _canSpin = true;
    public bool CanSpin { get { return _canSpin; } }
    // Start is called before the first frame update

    void Start()
    {

        _player = GameManager.Instance.SetPlayer();
        _enemyRB = GetComponent<Rigidbody2D>();
        _patrol= GetComponent<PatrolComponent>();
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        _animator.SetBool("FinishCharge", _finishCharge);
        _animator.SetBool("CanSpin", _canSpin);
        if (!_canSpin)
        {
            _currentTime += Time.deltaTime;
            if(_currentTime >= _spinCoolDown)
            {
                _canSpin = true;
                GetComponentInChildren<BoxCollider2D>().enabled = true;
            }
        }
        
    }
    public IEnumerator Spin()
    {
        _enabledSpinDamage = true;
        GetComponentInChildren<BoxCollider2D>().enabled = false;
        
        //Gira al detectar al jugador si no está orientado a este
        Vector3 _playerRelativePos = _player.transform.position - this.transform.position;
        if(_playerRelativePos.x < 0 && _patrol.lookingRight) 
        {
            _patrol.Turn();
        }
        else if(_playerRelativePos.x > 0 && !_patrol.lookingRight)
        {
            _patrol.Turn();
        }
        _finishCharge = false;
       
        _currentTime = 0;
        _inicialVelocity = _enemyRB.velocity;
        GetComponent<PatrolComponent>().enabled = false;
       
       
        
        yield return new WaitForSeconds(_timeCharge); //spin charge time

        _parSys.Play();
        // GetComponent<LifeEnemyComponent>().enabled = false;
        _finishCharge = true; //activa animacion spin
        _enemyRB.velocity = new Vector2(_spinVelocity * transform.localScale.x * (-1f), 0);

        yield return new WaitForSeconds(_timeSpin); //spin execution time

        
        _enemyRB.velocity = _inicialVelocity;
        //GetComponent<LifeEnemyComponent>().enabled = true;
        GetComponent<PatrolComponent>().enabled = true;
        _parSys.Stop();
        _canSpin = false;
        _enabledSpinDamage = false;

    }
}
