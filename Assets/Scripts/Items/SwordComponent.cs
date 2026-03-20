using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordComponent : MonoBehaviour
{
    //private GameObject _player;
    private Animator _animator;
    [SerializeField] private float _timeToPick;
    [SerializeField] private float _maxHeight;
    private float _currentHeight = 0;


    private float _counter = 0;
    private bool _canRise = true;


    private void Start()
    {
        //_player = GameManager.Instance.SetPlayer();
        _animator = GetComponent<Animator>();

    }
    private void Update()
    {

        if (_counter < _timeToPick)
        {
            _counter += Time.fixedDeltaTime;
            _currentHeight = _counter * 0.5f;
            if (_currentHeight < _maxHeight)
            {
                this.transform.position = new Vector2(transform.position.x, transform.position.y + _counter * 0.01f);
            }

        }
        else
        {
            _canRise = false;
        }
        _animator.SetBool("canRise", _canRise);

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_canRise && (bool)collision.gameObject.GetComponent<InputComponent>())
        {
            SoundComponent.Instance.PlaySound(SoundComponent.Instance._pickItem);
            GameManager.Instance.Sword();
            Destroy(gameObject);
        }
    }

}
