using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerCombat : MonoBehaviour
{

    private Animator _animator;

    [Header("ATTACK")] 
    [SerializeField] 
    private Transform _attackPoint;

    [SerializeField]
    private Vector2 _attackSize;

    private float _angleAttack= 90f;

    [SerializeField]
    private CapsuleDirection2D _direction;

    [SerializeField]
    private LayerMask _enemylayer;
    [SerializeField]
    private LayerMask _rangeLayer;

    [SerializeField]
    private int _attackDamage;

    

    [Header("AIR ATTACK")]

    [SerializeField]
    private float _radius;
    private float _gravIni;

    private void Start()
    {
      
        _animator = GetComponent<Animator>();
        
        
        _gravIni = GetComponent<Rigidbody2D>().gravityScale;
    }

    // play attack animation, detect enemies in range attack, damage them
    public void Attack()
    {
        if (GetComponent<MovementComponent>().TouchingFloor)
        {
            //Se desactiva input para que no se mueva mientras ataca
            GetComponent<InputComponent>().enabled = false;

            _animator.SetTrigger("Attack");
            SoundComponent.Instance.PlaySound(SoundComponent.Instance._playerAttack);

            Collider2D[] _hitEnemies = Physics2D.OverlapCapsuleAll(_attackPoint.position, _attackSize, _direction, _angleAttack, _enemylayer | _rangeLayer);

            foreach (Collider2D enemies in _hitEnemies)
            {
                if ((bool)enemies.GetComponent<BossManager>())
                {
                    enemies.GetComponent<BossManager>().ReceiveDamage(1);
                }
                else
                {
                    enemies.GetComponent<LifeEnemyComponent>().TakeDamage(_attackDamage);
                    enemies.GetComponent<RecoilComponent>().KnockBack(this.gameObject);
                }

            }
        }
        else
        {
            GetComponent<InputComponent>().enabled = false;
            GetComponent<Rigidbody2D>().gravityScale = 0;
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
          
            _animator.SetTrigger("Air");
            SoundComponent.Instance.PlaySound(SoundComponent.Instance._playerAirAttack);
            Collider2D[] _hitEnemisOnAir = Physics2D.OverlapCircleAll(_attackPoint.position, _radius, _enemylayer | _rangeLayer);



            
            foreach(Collider2D enemiesOnAir in _hitEnemisOnAir)
            {
                if ((bool)enemiesOnAir.GetComponent<BossManager>())
                {
                    enemiesOnAir.GetComponent<BossManager>().ReceiveDamage(1);
                }
                else
                {
                    enemiesOnAir.GetComponent<LifeEnemyComponent>().TakeDamage(_attackDamage);
                    enemiesOnAir.GetComponent<RecoilComponent>().KnockBack(this.gameObject);
                }             
            }
        }
    }
    //Se activa al final de la animacion de ataque
   
   
    private void OnDrawGizmosSelected()
    {
        if(_attackPoint == null)
        {
            return;
        }

    }
    //Llamado por un event al final de la animación
    public void ActivaInput()
    {
        GetComponent<InputComponent>().enabled = true;
        GetComponent<Rigidbody2D>().gravityScale = _gravIni;
    }


}
