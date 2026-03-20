using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_Movement : StateMachineBehaviour
{

    [SerializeField] private float _speed;
    private bool _lookingRight;
    [SerializeField] private float _attackDistance = 5f;

    [SerializeField] private float _maxAttackTimer;
    private float _currentAttackTime;

    private Transform _playerTransform;
    private Transform _myTransform;
    private Vector2 _playerPosition;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateinfo, int layerindex)
    {
        _playerTransform = GameManager.Instance.SetPlayer().transform;
        _myTransform = animator.transform;
        _currentAttackTime = 0;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _playerPosition = new Vector2(_playerTransform.position.x, animator.transform.position.y);

        if (Vector2.Distance(_playerTransform.transform.position, animator.transform.position) > _attackDistance)
        {
            animator.transform.position = Vector2.MoveTowards(animator.transform.position, _playerPosition, _speed * Time.deltaTime);
        }
        else
        {
            if (_currentAttackTime >= _maxAttackTimer) animator.SetTrigger("Attack");
        }

        _currentAttackTime += Time.deltaTime;
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateinfo, int layerindex)
    {
        animator.ResetTrigger("Attack");
    }

    

    
}
