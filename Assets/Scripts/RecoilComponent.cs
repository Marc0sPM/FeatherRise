using System.Collections;
using UnityEngine;


public class RecoilComponent : MonoBehaviour
{
    private Rigidbody2D _rb2D;
    [SerializeField] private float _recForce;
    [SerializeField] private float _delay;


    
    void Start()
    {
        _rb2D = GetComponent<Rigidbody2D>();
    }
    public void KnockBack(GameObject sender)
    {
        StopAllCoroutines();
        float individualDirection = 1;
        if ((bool)this.GetComponent<InputComponent>() || !(bool)sender.GetComponent<ProyectileComponent>())
        {
            individualDirection *= -1;
        }
        if (!(bool)this.GetComponent<InputComponent>())
        {
            individualDirection *= -1;
        }
        
        Vector2 direction;
        if (sender.transform.localScale.x > 0)
        {
            direction = new Vector2(individualDirection * _recForce, 3);
        }
        else
        {
            direction = new Vector2(-individualDirection * _recForce, 3);
        }
        EnabledDisabled(false); // Disable all needed components
        
        
        _rb2D.AddForce(direction * _recForce, ForceMode2D.Impulse);
        StartCoroutine(Reset());
    }
    private void EnabledDisabled(bool aux)
    {
        if ((bool)GetComponent<InputComponent>())
        {
            this.GetComponent<InputComponent>().enabled = aux;
            this.GetComponent<MovementComponent>().enabled = aux;
        }
        else
        {
            if ((bool)GetComponent<SpinComponent>())
            {
                this.GetComponent<SpinComponent>().enabled = aux;
                this.GetComponent<PatrolComponent>().enabled = aux;
            }
            
        }
    }


    public IEnumerator Reset()
    {
        yield return new WaitForSeconds(_delay);
        EnabledDisabled(true); //Enabled all needed components
        _rb2D.velocity = Vector3.zero;
    }
}
