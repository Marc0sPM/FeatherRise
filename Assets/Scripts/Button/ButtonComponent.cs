using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ButtonComponent : MonoBehaviour
{
    [SerializeField] private PlatformComponent _myPlatformComponent;
    private Animator _animator;

    private bool _buttonActivated = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_buttonActivated && (bool)collision.gameObject.GetComponent<FeatherComponent>() && collision.gameObject.GetComponentInParent<FeatherStates>().CurrrentState == FeatherStates.FeatherState.FEATHER)
        {
            SoundComponent.Instance.PlaySound(SoundComponent.Instance._button);
            _buttonActivated = !_buttonActivated;

            _myPlatformComponent.ChangeMove();
            gameObject.GetComponentInChildren<Light2D>().intensity = 2;

            collision.gameObject.GetComponentInParent<FeatherStates>().FreezeAutoReturn();
            collision.gameObject.GetComponentInParent<FeatherStates>().ChangeState(FeatherStates.FeatherState.PLATFORM);
        }
        else if (_buttonActivated && (bool)collision.gameObject.GetComponent<FeatherComponent>() && collision.gameObject.GetComponentInParent<FeatherStates>().CurrrentState == FeatherStates.FeatherState.FEATHER)
        {
            collision.gameObject.GetComponentInParent<FeatherStates>().ChangeState(FeatherStates.FeatherState.RETURN);
        }
    }
    private void Start()
    {
        _animator = GetComponent<Animator>();
    }
    private void Update()
    {
        _animator.SetBool("isActivated", _buttonActivated);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_buttonActivated && (bool)collision.gameObject.GetComponent<FeatherReturn>() && collision.gameObject.GetComponentInParent<FeatherStates>().CurrrentState == FeatherStates.FeatherState.RETURN)
        {
            _buttonActivated = !_buttonActivated;
            _myPlatformComponent.ChangeMove();
            gameObject.GetComponentInChildren<Light2D>().intensity = 1;
        }
    }

}
