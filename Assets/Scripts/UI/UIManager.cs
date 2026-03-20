using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.Jobs;
using UnityEngine.UI;
using JetBrains.Annotations;

public class UIManager : MonoBehaviour
{
    #region references
    [SerializeField] private TMP_Text _move;
    [SerializeField] private TMP_Text _jump;
    [SerializeField] private TMP_Text _interact;
    [SerializeField] private TMP_Text _feather;
    [SerializeField] private TMP_Text _dash;
    [SerializeField] private TMP_Text _sword;
    [SerializeField] private TMP_Text _button;
    //[SerializeField] private Image _bloodEffect;

    
    public GameObject[] _souls;
    public GameObject[] _lifes;
    public GameObject[] _feathers;

    



    //MENUS
    //[SerializeField] private GameObject _startMenu;
    //[SerializeField] private GameObject _gameplayHUD;
    [SerializeField] private GameObject _pauseMenu;
    //[SerializeField] private GameObject _gameOverMenu;
    [SerializeField] private AudioMixer _audioMixer;
    #endregion

    #region properties
    //array de distintos menus
    private GameObject[] _menus;
    //private float r;
    //private float g;
    //private float b;
    //private float a;
    
    #endregion
    #region methods MenuInicial
    public void Play()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Time.timeScale = 1f;
    }
    public void Salir()
    {
        Debug.Log("salir");
        Application.Quit(); 
    }
    #endregion
    #region methods
    public void QuitSouls(int cant)//ayuda cuando queda una vida ns q hacer
    {
        if(cant == 1)
        {
            _souls[GameManager.Instance._soul1 -1].SetActive(false);
        }
        else
        for (int i = 0; i < cant; i++)
        {
            _souls[2 - i].SetActive(false);
        }
        
    }
    public void Addsouls(int cant)
    {
        for (int i = 0; i < cant; i++)
        {
            
            _souls[2 - i].SetActive(true);
        }
    }
    public void QuitLifes(int cant)
    {
        for(int i = 0; i < cant; i++)
        {
            if(cant == 1)
            {
                _lifes[0].SetActive(false);
            }
            else
            _lifes[1 - i].SetActive(false);
        }
       
    }
    public void AddLifes(int ind)
    {
        if(GameManager.Instance.Lifes > -1)
        {
            _lifes[ind].SetActive(true);
        }
        
    }

    public void AddFeathers(int ind)
    {
        _feathers[ind - 1].SetActive(true);
    }
    public void QuitFeathers(int ind)
    {
        _feathers[ind].SetActive(false);
    }
    public void QuitAllLifes()
    {
        for(int i = 0; i < _lifes.Length; i++)
        {
            _lifes[i].SetActive(false);
        }
    }
    public void AddAllSouls()
    {
        _souls[0].SetActive(true);
        _souls[1].SetActive(true);
        _souls[2].SetActive(true);
    }
    public void AddAllFeathers()
    {
        _feathers[0].SetActive(true);
        _feathers[1].SetActive(true);
        _feathers[2].SetActive(true);

    }
    public void Evalue()
    {
        Addsouls(GameManager.Instance._souls);
        if(GameManager._lifes != -1)
        {
            AddLifes(GameManager._lifes);
        }
        if(GameManager._lifes == -1)
        {
            QuitLifes(GameManager._lifes);
        }
        
        Debug.Log(_lifes);
    }
    #endregion
    #region methods MenuOpciones
    public void FullScreen(bool _fullScreen)
    {
        Screen.fullScreen = _fullScreen;
    }
    public void ChangeVolumeMusic(float _volume)
    {
        _audioMixer.SetFloat("Music", _volume);
    }
    public void ChangeVolumeFX(float _volumeFx)
    {
        _audioMixer.SetFloat("Effects", _volumeFx);
    }
    public void ChangeVolumeGeneral(float _volumeGeneral)
    {
        _audioMixer.SetFloat("Volumen", _volumeGeneral);
    }
    public void ChangeQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }
    #endregion
    #region methods MenuPause
    public void Pause()
    {
        
        Time.timeScale = 0f;
        _pauseMenu.SetActive(true);      
    }
    public void Reanudar()
    {
        Time.timeScale = 1f;
        _pauseMenu.SetActive(false);
    }
    public void Quit()
    {
        SceneManager.LoadScene(0);
    }
    #endregion
    #region MenuSeleccion
    public void ChangeLevels (int level)
    {
        SceneManager.LoadScene(level);
        Time.timeScale = 1f;
    }
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        //r = _bloodEffect.color.r;
        //g = _bloodEffect.color.g;
        //b = _bloodEffect.color.b;
        //a = _bloodEffect.color.a;
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (GameManager.Instance.Soul == 1 && a < 0.05f)
        //{

        //    a += 0.005f;
        //}
        //else if(GameManager.Instance.Soul > 1)
        //{
        //    a = 0;
        //}

        //a = Mathf.Clamp(a, 0f, 1f);
        //ChangeColor();
        
    }
    //private void ChangeColor()
    //{
    //    Color c = new Color(r, g, b, a);
    //    _bloodEffect.color = c;
        
    //}
}
