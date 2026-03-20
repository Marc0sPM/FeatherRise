using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameManager;

public class BossManager : MonoBehaviour
{
    [Header ("Teleport")]
    [SerializeField] private int _teleportCounter;      // Quitar SerializeField
    [SerializeField] private int _teleportsDesired;

    [Header("Enemies")]
    [SerializeField] private int _enemiesCounter;       // Quitar SerializedField
    [SerializeField] private int _enemiesDesired;

    [Header("Trunk")]
    [SerializeField] private int _trunkCounter;         // Quitar Serialized Field
    [SerializeField] private int _trunkDesired;

    [Header("Final")]
    [SerializeField] private int _finalCounter;         // Quitar Serialized Field
    [SerializeField] private int _finalDesired;

    [Header("Hits Received")]
    [SerializeField] private float _maxHitsReceivedForTeleport;
    [SerializeField] private float _maxHitsReceivedForEnemy;
    [SerializeField] private float _maxHitsReceivedForTrunk;
    [SerializeField] private float _maxHitsReceivedForFinal;
    [SerializeField] private float _auxHitsReceived;
    [SerializeField] private LifeBoss _lifeBoss;
    [SerializeField] private float _totalHits;


    [SerializeField] private float _currentHitsReceived;       //Quitar SerializeField

    private BossSimpleAttack _myBossSimpleAttackComponent;
    private Animator _myAnimator;
    private bool _throw = false;
    [SerializeField] private float _throwCoolDown;
    private float _counter = 0;

    [Header("LookingAt")]
    //Variables del direccion
    private bool _lookingRight;
    private Transform _playerTransform;
    [SerializeField] private float _turningDistance;

    #region Methods
    public void ReceiveDamage(int damageReceived)
    {
        Debug.Log("-1 vida boss");
        _currentHitsReceived += damageReceived;
        _totalHits += damageReceived;
        _lifeBoss.ChangeCurrentLife(((_teleportsDesired - 1) * _maxHitsReceivedForTeleport + _maxHitsReceivedForEnemy + (_trunkDesired) * (_maxHitsReceivedForTrunk - _maxHitsReceivedForEnemy) + (_maxHitsReceivedForFinal - _maxHitsReceivedForTrunk)) - _totalHits);
        GetComponent<iFramesComponent>().IFrames();
        SoundComponent.Instance.PlaySound(SoundComponent.Instance._bossReceiveDamage);
        CheckAction();
    }
    private void CheckAction()      // Cada vez que recibamos daño, verá que acción deberá hacer el boss
    {
        if (_currentHitsReceived == _maxHitsReceivedForTeleport && _teleportCounter < _teleportsDesired)
        {
            _teleportCounter++;
            _myAnimator.SetTrigger("Teleport");
            if (_teleportCounter != _teleportsDesired)
            {
                _currentHitsReceived = 0;
            }
            else
            {
                _auxHitsReceived = _currentHitsReceived;
                _myAnimator.SetBool("isPhase1", false);
                _myAnimator.SetBool("isPhase2", true);
                _throw = _myAnimator.GetBool("isPhase2");
            }
            //if(_totalHits == 3*_maxHitsReceivedForTeleport)
            //{
            //    _lifeBoss.InitialLife(_maxHitsReceivedForEnemy);
            //}
        }

        else if (_currentHitsReceived == _maxHitsReceivedForEnemy && _enemiesCounter < _enemiesDesired)
        {
            _enemiesCounter++;
            
            if (_enemiesCounter != _enemiesDesired)
            {
                _currentHitsReceived = _auxHitsReceived;
            }
            else
            {
                _auxHitsReceived = _currentHitsReceived;
            //    _lifeBoss.ChangeCurrentLife(_maxHitsReceivedForTrunk - _currentHitsReceived);
                _myAnimator.SetBool("isPhase2", false);
                _myAnimator.SetBool("isPhase3", true);
                _throw = _myAnimator.GetBool("isPhase2");
            }
        }
        else if (_currentHitsReceived == _maxHitsReceivedForTrunk && _trunkCounter < _trunkDesired)
        {
            _trunkCounter++;
            _myAnimator.SetTrigger("Throw Trunk");
            if (_trunkCounter != _trunkDesired)
            {
                _currentHitsReceived = _auxHitsReceived;
            }
            else
            {
                _auxHitsReceived = _currentHitsReceived;
                _myAnimator.SetBool("isPhase3", false);
                _myAnimator.SetBool("isPhase4", true);
            }
        }
        else if (_currentHitsReceived == _maxHitsReceivedForFinal && _finalCounter < _finalDesired)
        {
            _finalCounter++;
            if (_finalCounter != _finalDesired)
            {
                _currentHitsReceived = _auxHitsReceived;
            }
            else
            {
                _auxHitsReceived = _currentHitsReceived;
                _myAnimator.SetBool("isPhase4", false);
                _myAnimator.SetBool("isDead", true);
            }
        }
    }

    public void BossDeath()
    {
        Destroy(gameObject);
        SceneManager.LoadScene(5);
    }
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        _myAnimator = GetComponent<Animator>();
        _playerTransform = GameManager.Instance.SetPlayer().transform;
        _myBossSimpleAttackComponent = GetComponent<BossSimpleAttack>();
        _lifeBoss.InitialLife(((_teleportsDesired -1) * _maxHitsReceivedForTeleport + _maxHitsReceivedForEnemy + (_trunkDesired) *(_maxHitsReceivedForTrunk - _maxHitsReceivedForEnemy) + (_maxHitsReceivedForFinal - _maxHitsReceivedForTrunk)));
    }

    // Update is called once per frame
    void Update()
    {
        if (_throw)
        {
            if(_counter < _throwCoolDown) _counter += Time.deltaTime;
            else
            {
                Debug.Log("Lanza enemigo");
                _myAnimator.SetTrigger("Throw Enemy");
                _counter = 0;
            }
        }
        if (Vector2.Distance(_playerTransform.transform.position, _myAnimator.transform.position) > _turningDistance)
        {
            LookingPlayer(_myAnimator);
        }

    }

    private void LookingPlayer(Animator animator)
    {
        Debug.Log("Looking player");

        if (animator.transform.position.x < _playerTransform.position.x && !_lookingRight)
        {
            _lookingRight = true;
            Turn(animator);
        }

        if (animator.transform.position.x > _playerTransform.position.x && _lookingRight)
        {
            _lookingRight = false;
            Turn(animator);
        }
    }
    private void Turn(Animator animator)
    {
        Vector3 scale = animator.transform.localScale;
        scale.x *= -1;
        animator.transform.localScale = scale;
        _myBossSimpleAttackComponent.isTurned();
    }
}
