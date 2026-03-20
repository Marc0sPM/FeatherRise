using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class iFramesComponent : MonoBehaviour
{
    [SerializeField] private float _time; //tiempo para que pueda volver a recibir daño
    [SerializeField] private int _flashes;
    //[SerializeField] private LayerMask _enemyLayer;

    private SpriteRenderer _mySpriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        _mySpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void IFrames()
    {
        if ((bool)GetComponent<BossManager>())
        {
            GetComponent<PolygonCollider2D>().enabled = false;
            StartCoroutine(IFrameProcess());
        }
        else
        {
            StartCoroutine(IFrameProcess());            
        }
    }
    private IEnumerator IFrameProcess()
    {
        for (int i = 0; i < _flashes; i++)
        {
            Physics2D.IgnoreLayerCollision(7, 9, true);      //Desactivamos las colisiones entre el player y el enemigo
            GetComponent<ShowDamage>().StartCoroutine(GetComponent<ShowDamage>().ModSprite());      //Modificamos el sprite del player
            yield return new WaitForSeconds(_time);                             
            Physics2D.IgnoreLayerCollision(7, 9, false);     //Volvemos a activar las colisiones 
            if ((bool)GetComponent<BossManager>())
            {
                GetComponent<PolygonCollider2D>().enabled = true;
            }
            else
            {
                GameManager.Instance.CanTakeDamage = true;
            }
        }
    }
}
