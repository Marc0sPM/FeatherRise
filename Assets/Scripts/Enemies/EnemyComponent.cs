using Unity.VisualScripting;
using UnityEngine;

public class EnemyComponent : MonoBehaviour
{
    //Inflinge daño al player

    
    

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        if ((bool)collision.gameObject.GetComponent<InputComponent>())
        {
            
            if ((bool)this.GetComponent<SpinComponent>() && this.GetComponent<SpinComponent>().EnableSpinDamage)
            {
                GameManager.Instance.Loselifes(2, this.gameObject);
            }
            else
            {
                
                GameManager.Instance.Loselifes(1, this.gameObject); 
            }
            
        }
    }
}
