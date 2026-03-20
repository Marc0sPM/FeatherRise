using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProyectileComponent : MonoBehaviour
{
    [SerializeField] private float _timer;
    [SerializeField] private float _maxTime;
    [SerializeField] private ParticleSystem _parSys;
    [SerializeField] private ParticleSystem _explossionSys;
    private bool _canDamage = true;
    
    private void Start()
    {
        _parSys.Play();
    }
    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;
        if(_timer > _maxTime)
        {
            Destroy(gameObject);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        if ((bool)collision.gameObject.GetComponent<InputComponent>() && _canDamage)
        {
            Destroy(gameObject);
            GameManager.Instance.Loselifes(1, this.gameObject);
        }
        if ((bool)collision.gameObject.GetComponent<TilemapCollider2D>() || (bool)collision.gameObject.GetComponent<FeatherWallCol>()) 
        {
            _canDamage = !_canDamage;
            _explossionSys.Play();
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            if(_explossionSys.isStopped)
            {
                Destroy(gameObject);
            }
           
        }
    }
    
}
