using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicCaller : MonoBehaviour
{
    [SerializeField] private MusicComponent _myMusicComponent;
    [SerializeField] private bool _OST1;
    [SerializeField] private bool _OST2;
    [SerializeField] private bool _OST3;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((bool)collision.GetComponent<InputComponent>())
        {
            Debug.Log("Jugador debe cambiar musica");
            if (_OST1)
            {
                _myMusicComponent._playOST1 = true;
            }
            if (_OST2)
            {
                _myMusicComponent._playOST2 = true;
                Debug.Log("Cambio a OST2");
            }
            if (_OST3)
            {
                _myMusicComponent._playOST3 = true;
            }
        }
    }
}