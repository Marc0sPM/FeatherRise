
using UnityEngine;
using UnityEngine.SceneManagement;

public class VoidComponent : MonoBehaviour
{
    RespawnComponent _myRespawnComponent;
    private void Start()
    {
        _myRespawnComponent = GetComponent<RespawnComponent>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((bool)collision.gameObject.GetComponent<InputComponent>())
        {
            Debug.Log("ColRes");
            var player = collision.gameObject;
            player.GetComponent<RespawnComponent>().Respawn();
            if (GameManager._lifes >= 0)
            {
                GameManager._lifes = -1;
                GameManager.Instance.EvalueG();
            }

            Tracker.Instance.TrackEvent(new Player_Death(player.transform.position.x, player.transform.position.y, "void"));
        }
        if ((bool)collision.gameObject.GetComponent<LifeEnemyComponent>())
        {
            Destroy(collision.gameObject);
        }
        
    }
}

    
