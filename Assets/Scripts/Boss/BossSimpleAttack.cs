using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class BossSimpleAttack : MonoBehaviour
{
    [SerializeField] private int _attackDamage = 1;

    [SerializeField] private Vector3 _attackOffset;
    [SerializeField] private float _attackRadius;
    [SerializeField] private LayerMask _myLayerMask;
    private int _multiplier = 1;
    public void SimpleAttack1()
    {
        Vector3 attackPosition = transform.position;
        attackPosition += transform.right * _attackOffset.x * _multiplier;
        attackPosition += transform.up * _attackOffset.y;

        Collider2D myCollision = Physics2D.OverlapCircle(attackPosition, _attackRadius, _myLayerMask);
        if (myCollision != null)
        {
            SoundComponent.Instance.PlaySound(SoundComponent.Instance._bossAttack);
            GameManager.Instance.Loselifes(1, this.gameObject);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        pos += transform.right * _attackOffset.x * _multiplier;
        pos += transform.up * _attackOffset.y;

        Gizmos.DrawWireSphere(pos, _attackRadius);
    }

    public void isTurned()
    {
        Debug.Log("Cambio dirección");
        _multiplier *= -1;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
