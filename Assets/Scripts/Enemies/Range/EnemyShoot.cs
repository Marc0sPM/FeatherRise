using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    [SerializeField] private GameObject _projectile;
    [SerializeField] private float _force;
    private GameObject _player;
    private ShowRay _showRay;
    

    
    private bool _lookingRight = true;
    public bool LookingRight { get { return _lookingRight; } }


    // Start is called before the first frame update
    void Start()
    {
        _player = GameManager.Instance.SetPlayer();
        _showRay= GetComponent<ShowRay>();
    }

    
   
    public void Shoot()
    {
        Vector3 _playerRelativePos = _player.transform.position - this.transform.position;
        if (_playerRelativePos.x < 0 && !_lookingRight)
        {
            Turn();
        }
        else if (_playerRelativePos.x > 0 && _lookingRight) Turn();

        Vector3 direction = _player.transform.position - transform.position;
        float rot = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
        GameObject arrow = Instantiate(_projectile, transform.position, Quaternion.Euler(0,0,rot+180));
        
        if (transform.localScale.x < 0)
        {
            arrow.GetComponent<Rigidbody2D>().AddForce(new Vector2( direction.x, direction.y)*_force, ForceMode2D.Force);
        }
        else
        {
            arrow.GetComponent<Rigidbody2D>().AddForce(new Vector2(direction.x, direction.y)*_force, ForceMode2D.Force);
        }

        

    }
    public void Turn()
    {
        _lookingRight = !_lookingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
