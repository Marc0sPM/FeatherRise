using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrunkDetectionComponent : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _myRigidbody2D;
    [SerializeField] private WoodComponent _woodComponent;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if ((bool)collision.GetComponent<InputComponent>() && _woodComponent.CanFall)
        {
            _myRigidbody2D.gravityScale = 2;
            _woodComponent.CanFall = false;
            _woodComponent.Falling = true;
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
