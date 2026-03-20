using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WoodComponent : MonoBehaviour
{
    private Rigidbody2D _myRigidbody2D;

    private bool _falling = false;
    private bool _returning = false;
    [SerializeField] private float _waitingSeconds = 0.5f;
    public bool Falling { get { return _falling; } set { _falling = value; } }

    private bool _canFall = true;
    public bool CanFall { get { return _canFall; } set { _canFall = value; } }

    [SerializeField] private float _returningVelocity;
    [SerializeField] private Transform _limitPoint;
    private Vector2 _startPosition;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((bool)collision.GetComponent<InputComponent>())
        {
            collision.GetComponent<RespawnComponent>().Respawn();
        }
    }
    private IEnumerator ChangeToReturn()
    {
        GetComponent<BoxCollider2D>().enabled = false;
        _myRigidbody2D.gravityScale = 0;
        _myRigidbody2D.velocity = Vector2.zero;

        yield return new WaitForSeconds(_waitingSeconds);

        _myRigidbody2D.gravityScale = -1;
        _returning = true;
    } 
    // Start is called before the first frame update
    void Start()
    {
        _startPosition = transform.position;
        _myRigidbody2D = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_falling)
        {
            if (transform.position.y <= _limitPoint.position.y)
            {
                SoundComponent.Instance.PlaySound(SoundComponent.Instance._trunkHit);
                _falling = false;
                StartCoroutine(ChangeToReturn());                
            }
        }
        if (_returning)
        {
            if (transform.position.y >= _startPosition.y)
            {
                _myRigidbody2D.gravityScale = 0;
                _myRigidbody2D.velocity = Vector2.zero;
                _canFall = true;
                _returning = false;
                GetComponent<BoxCollider2D>().enabled = true;
            }
        }
    }
}
