using System.Collections;
using UnityEngine;

public class ShowDamage : MonoBehaviour
{
    [SerializeField] private float _timeOfDamage; //tiempo que se muestra el enemigo en rojo para saber que se ha hehco daño
                                                  //Optimo ponerlo a 0.2
    private SpriteRenderer _spriteRenderer;
    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public IEnumerator ModSprite()
    {
        _spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(_timeOfDamage);
        _spriteRenderer.color = Color.white;

    }
}
