
using UnityEngine;

public class NormalWallColision : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if((bool)collision.gameObject.GetComponent<FeatherComponent>())
        {
            collision.gameObject.GetComponentInParent<FeatherStates>().ChangeState(FeatherStates.FeatherState.RETURN);
        }
    }
}