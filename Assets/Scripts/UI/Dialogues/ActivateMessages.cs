using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateMessages : MonoBehaviour
{
    private GameObject _player;
    public Textos text;
    [SerializeField]private DialogueControl _dialogueControl;
    [SerializeField] private GameObject _message;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((bool)collision.gameObject.GetComponent<MovementComponent>())
        {
             _player.GetComponent<InputComponent>().enabled = false;
            _player.GetComponent<MovementComponent>().Move(0);
            _dialogueControl.MessageActive(text);
            
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if ((bool)collision.gameObject.GetComponent<MovementComponent>())
        {
            _message.SetActive(false);

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _player = GameManager.Instance.SetPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
