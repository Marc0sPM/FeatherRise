using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingPlatformComponent : MonoBehaviour
{
    [Header("FALLING")]
    [SerializeField]
    private float _delay;
    [SerializeField]
    private float _amountOfShakes;
    [SerializeField]
    private GameObject _myPlatform;
    private Transform _myPlatformTransform;
    private Rigidbody2D _myPlatformRB;

    //booleanos de control
    private bool _readyToShake;
    private bool _fall;
    //Vector auxiliar para la posicion inicial
    private Vector2 _myOriginalPos;


    // Start is called before the first frame update
    void Start()
    {
        _myPlatformRB = _myPlatform.GetComponent<Rigidbody2D>();
        _myPlatformTransform = _myPlatform.transform;
        
        _myOriginalPos = _myPlatform.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (_readyToShake)
        {
            Vector2 _shakePosition = _myPlatformTransform.position + Random.insideUnitSphere * (Time.deltaTime * _amountOfShakes);

            _myPlatformTransform.position = _shakePosition;
        }
        else if (_fall)
        {
            _myPlatformRB.constraints = RigidbodyConstraints2D.None;
            _myPlatformRB.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
    }

    
    public IEnumerator Falling()
    {
        Debug.Log("Enumerator");
        yield return new WaitForSeconds(_delay);

        _readyToShake = true;


        yield return new WaitForSeconds(_delay);

        _readyToShake = false;
        _fall = true;

        //desactivamos los colliders para que no colisionen
        
        _myPlatform.GetComponent<BoxCollider2D>().enabled = false;
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        
        yield return new WaitForSeconds(3f);

        //Destroy(_myPlatform);

        _myPlatform.GetComponent<BoxCollider2D>().enabled = true;
        gameObject.GetComponent<BoxCollider2D>().enabled = true;
        _myPlatformRB.constraints = RigidbodyConstraints2D.FreezeAll;
        _fall = false;
        _myPlatformTransform.position = _myOriginalPos;


    }
    
}
