using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShowTutorialImages2 : MonoBehaviour
{
    private bool _canDestroy = false;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if ((bool)collision.GetComponent<InputComponent>() && GameManager.Instance.FeatherCant >= 1)
        {
            GetComponent<SpriteRenderer>().enabled = true;
            GetComponent<Light2D>().enabled = true;
            _canDestroy = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if ((bool)collision.GetComponent<InputComponent>() && _canDestroy)
        {
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Light2D>().enabled = false;
            Destroy(gameObject);
        }
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
