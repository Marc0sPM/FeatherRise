using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using Unity.VisualScripting;
using UnityEngine;


public class FeatherReturn : MonoBehaviour
{
    private Rigidbody2D _rb2D;
    private GameObject _player;

    [SerializeField] private float _returnSpeed;
    private bool _canReturn = false;

    // Start is called before the first frame update
    void Start()
    {
        _rb2D = GetComponentInParent<Rigidbody2D>();
        _player = GameManager.Instance.SetPlayer();
    }
   
    // Update is called once per frame
    void Update()
    {
        if (_canReturn)
        {
            Return(_player.transform.position);
        }
    }
    public void ActivateReturn()
    {
        _canReturn = !_canReturn;
    }
    private void Return(Vector3 endPos)
    {
        Vector3 direction = endPos - transform.position;
        _rb2D.velocity = new Vector3(direction.x, direction.y).normalized * _returnSpeed;
        
    }
}
