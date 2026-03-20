using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class FeatherStates : MonoBehaviour
{
    public enum FeatherState {FEATHER, PLATFORM, RETURN}
    private GameObject _player;

    [SerializeField]private FeatherState _currentState;
    public FeatherState CurrrentState { get { return _currentState; } }
   
    [SerializeField]private FeatherState _nextState;

    private void EnterState(FeatherState state)
    {

        
        switch (state)
        {
            case FeatherState.FEATHER:
                SoundComponent.Instance.PlaySound(SoundComponent.Instance._featherThrow);
                this.gameObject.transform.GetChild((int)FeatherState.FEATHER).gameObject.SetActive(true);
                
                break;
            case FeatherState.PLATFORM:

                SoundComponent.Instance.PlaySound(SoundComponent.Instance._featherPlatform);
                //seteamos todo lo que no vaya a interferir con el estado antes de cambiar para que no haya errores en la maquina de estados
                this.gameObject.transform.GetChild((int)FeatherState.FEATHER).gameObject.SetActive(false);
                //Se realiza lo que verdaderamente hace el estado
                this.gameObject.transform.GetChild((int)FeatherState.PLATFORM).gameObject.SetActive(true);
                GetComponent<Rigidbody2D>().bodyType =  RigidbodyType2D.Static;
                transform.rotation = Quaternion.identity;
                

                break;
          
            case FeatherState.RETURN:
                //seteamos todo lo que no vaya a interferir con el estado antes de cambiar para que no haya errores en la maquina de estados
                SoundComponent.Instance.PlaySound(SoundComponent.Instance._featherReturn);
                GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                this.gameObject.transform.GetChild((int)FeatherState.FEATHER).gameObject.SetActive(false);
                this.gameObject.transform.GetChild((int)FeatherState.PLATFORM).gameObject.SetActive(false);
                //Se realiza lo que verdaderamente hace el estado
                this.gameObject.transform.GetChild((int)FeatherState.RETURN).gameObject.SetActive(true);
                transform.rotation = this.gameObject.transform.GetChild((int)FeatherState.FEATHER).gameObject.GetComponent<FeatherComponent>().Rotation;
                this.gameObject.transform.GetChild((int)FeatherState.RETURN).gameObject.GetComponent<FeatherReturn>().ActivateReturn();
                break;
        }
    }
    public void FreezeAutoReturn()
    {
        _player.GetComponentInChildren<FeatherDetectionComponent>().enabled = false;
    }
    public void ChangeState(FeatherState newState)
    {
        _nextState = newState;
    }
    private void Awake()
    {
        this.gameObject.transform.GetChild((int)FeatherState.FEATHER).gameObject.SetActive(false);
        this.gameObject.transform.GetChild((int)FeatherState.PLATFORM).gameObject.SetActive(false);
        this.gameObject.transform.GetChild((int)FeatherState.RETURN).gameObject.SetActive(false);
        _player = GameManager.Instance.SetPlayer();
    }
    // Start is called before the first frame update
    void Start()
    {
        
        _currentState = FeatherState.PLATFORM;
        _nextState = FeatherState.FEATHER;
        
    }

    // Update is called once per frame
    void Update()
    {
        if(GameManager.Instance.FeatherCant <= 0 && Input.GetButtonDown("Feather Return") && _currentState == FeatherState.PLATFORM)
        {
            _nextState = FeatherState.RETURN;
        }
        if(_nextState != _currentState)
        {
            if (_nextState == FeatherState.PLATFORM) FreezeAutoReturn(); //Sirve para desactivar el Return que tiene en player en FeatherRange
            _currentState = _nextState;
            EnterState(_currentState);
        }
        
    }
}
