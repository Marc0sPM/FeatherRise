using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class RespawnComponent : MonoBehaviour
{
    
    private float _gravIni;
    private Rigidbody2D _rb;
    private float _timeAnim;
    [SerializeField] private float _delay;
    private bool _spawning = false;
    
    void Start()
    {
        _gravIni = GetComponent<Rigidbody2D>().gravityScale;
        _rb = GetComponent<Rigidbody2D>();
        
    }
    private void Update()
    {
        if (GameManager.Instance.IsDeath)
        {
            StartCoroutine("Res");
            Die(); 
            if (GetComponent<MovementComponent>().TouchingFloor || _spawning)
            {
                EnablePhysics();
                Physics2D.IgnoreLayerCollision(7, 9, true);
                _timeAnim += Time.deltaTime;
                if (_timeAnim > 1f)
                {
                    _spawning = false;
                    Respawn();
                    _timeAnim = 0f;
                }
            }
            
        }
    }

    public void Die() //Desactiva componentes de player que interactuan con el mapa
    {

        GetComponent<InputComponent>().enabled = false;
        DisablePhysics();
        _rb.gravityScale = 6f;
        _rb.velocity = Vector2.zero;
       
    }
    private void DisablePhysics()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
    }
    private void EnablePhysics()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
    }
    
    

    public void Respawn() //Llamar cuando tengamos HUD y esas vainas
    {
        SoundComponent.Instance.PlaySound(SoundComponent.Instance._respawn);
        Physics2D.IgnoreLayerCollision(7, 9, false);
        GetComponent<InputComponent>().enabled = true;
        _rb.gravityScale = _gravIni;
        GameManager.Instance.Respawne();
        GameManager.Instance.ResetSouls();
        GameManager.Instance.ResetLifes();
        Debug.Log("Resp");

    }
    
    private IEnumerator Res()
    {
        Debug.Log("Enumerator");

        yield return new WaitForSeconds(_delay);

        _spawning = true;
    }
}
