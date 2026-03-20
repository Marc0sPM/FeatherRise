using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformComponent : MonoBehaviour
{
    [SerializeField] private bool _moveTowardsTarget = false;
    public bool MoveTowardsTarget { get { return _moveTowardsTarget; } }

    [SerializeField] private Transform _targetPosition;
    public Transform TargetPosition { get { return _targetPosition; } }
    private Vector2 _startPosition;

    [SerializeField] private float _speed;

    [Header("Collider")]

    [SerializeField] private Transform _colliderController;
    [SerializeField] private Vector2 _colliderDimensions;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private GameObject _player;
    private bool _touching;
    // Start is called before the first frame update
    void Start()
    {
        _startPosition = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _touching = Physics2D.OverlapBox(_colliderController.position, _colliderDimensions, 0, _playerLayer);
        if(_touching)
        {
            _player.transform.SetParent(this.transform);
        }
        else _player.transform.SetParent(null);

        if (_moveTowardsTarget)
        {
            PlatformBehaviour(_targetPosition.position);
        }
        else
        {
            PlatformBehaviour(_startPosition);
        }
    }
    public void ChangeMove()
    {
        _moveTowardsTarget = !_moveTowardsTarget;
    }

    void PlatformBehaviour(Vector2 targetPosition)
    {
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, _speed * Time.deltaTime);
    }
    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if ((bool)collision.gameObject.GetComponent<InputComponent>())
    //    {
    //        collision.transform.SetParent(this.transform);
    //    }
    //}
    //private void OnCollisionExit2D(Collision2D collision)
    //{
    //    if ((bool)collision.gameObject.GetComponent<InputComponent>())
    //    {
    //        collision.transform.SetParent(null);
    //    }
    //}
}
