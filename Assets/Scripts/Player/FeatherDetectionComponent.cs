using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatherDetectionComponent : MonoBehaviour
{
    private void OnTriggerExit2D(Collider2D collision)
    {
        if ((bool)collision.gameObject.GetComponent<FeatherComponent>() && collision.gameObject.GetComponentInParent<FeatherStates>().CurrrentState != FeatherStates.FeatherState.PLATFORM)
        {
            collision.gameObject.GetComponentInParent<FeatherStates>().ChangeState(FeatherStates.FeatherState.RETURN);
        }
    }
}
