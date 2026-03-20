using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class CheckReturn : MonoBehaviour
{
    //metodo para que, al volver la pluma, el jugador pueda reutilizarla (llama al GameManager para sumarle 1)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if((bool)collision.gameObject.GetComponent<FeatherReturn>())
        {
            GameManager.Instance.AddFeather();
            Destroy(collision.transform.parent.gameObject);
        }
    }
}
