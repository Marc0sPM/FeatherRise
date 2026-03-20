using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorComponent : MonoBehaviour
{
    [Header("INDEX")]
    [SerializeField]
    private int _myIndex;
    int _lifes = GameManager._lifes;


    //metodo que detecta al player y si le da al boton de interactuar
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<InputComponent>() && Input.GetButton("Interact"))
        {
            CambiarNivel(_myIndex);
        }
    }
    //metodo publico para cambiar el nivel al interactuar con la puerta
    public void CambiarNivel(int _index)
    {

        SceneManager.LoadScene(_index);
        GameManager.Instance.Featherslvl2();
        
    }
}
