using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeDeteccion : MonoBehaviour
{
    private ShowRay _showRay;
    [Header("Range")]

    [SerializeField] private Transform _rangeControler; //controlador del suelo
    [SerializeField] private float _rangeRadius;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _timeToShoot;
    [SerializeField] private float _shootCooldown;

    [Header("Rayo")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Transform _startTransform;
    [SerializeField] private float _range;
    [SerializeField] private LineRenderer _lineRenderer;
    private Transform _myTransform;

    private Animator _animator;
    private bool _detection;
    

    private EnemyShoot _projectile;
    // Start is called before the first frame update
    void Start()
    {
        _showRay= GetComponent<ShowRay>();
        _projectile= GetComponent<EnemyShoot>();
        _animator = GetComponent<Animator>();
        _myTransform = transform;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        _detection = Physics2D.OverlapCircle(_rangeControler.position, _rangeRadius , _playerLayer);
    }
    private void Update()
    {
       if(_detection && !_showRay.StopRay)
       {
            RaycastHit2D _raycastHit2D = Physics2D.Raycast(_myTransform.position, _playerTransform.position, Mathf.Infinity, _playerLayer);
            _lineRenderer.startColor = Color.red;
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, _startTransform.position);
            _lineRenderer.SetPosition(1, _playerTransform.position);
       }
        else if(!_detection) 
        {
            _lineRenderer.enabled = false;
        }
        if (_detection && _timeToShoot > _shootCooldown )
        {
            Debug.Log("Detectado");

            _animator.SetTrigger("ShootCharge");
            
            _timeToShoot = 0;
           
        }
        if(_timeToShoot <= _shootCooldown)
        {
            
            _timeToShoot += Time.deltaTime;
        }
    }
    public void StopShoot()
    {
        _animator.SetTrigger("StopShoot");
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_rangeControler.position, _rangeRadius);
    }
}
