using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CheckPointComponent : MonoBehaviour
{
    private Light2D _light;
  
    [SerializeField] private float _intensityMax;
    [SerializeField] private Color _color;
    [SerializeField] private Transform _spawnPoint;

    private GameObject _particleSys;
    
    private void Start()
    {
        _particleSys = GetComponentInChildren<ParticleSystem>().gameObject ;
        _particleSys.SetActive(false);
        
        
        _light = GetComponentInChildren<Light2D>();
       
    }
    //al entrar en el checkpoint detecta que lo que ha entrado es el player e inicializa la posicion del spawn donde el checkpoint (GameManager)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((bool)collision.gameObject.GetComponent<InputComponent>())
        {
            GameManager.Instance.Checkpoint(_spawnPoint.position);
            //stablish new light settings
            _light.intensity = _intensityMax;
            _light.color = _color;
            

            //Activate particles
            _particleSys.SetActive(true);
            SoundComponent.Instance.PlaySound(SoundComponent.Instance._checkPoint);
            GetComponent<Collider2D>().enabled = false;

        }
    }
}
