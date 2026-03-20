using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SkinManager : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    private SpriteRenderer _renderer;
    [SerializeField] private Color[] _colorSelector;
    
    private Color _color;
    public Color Col { get { return _color; } } 
    private int _cIndex = 0;
    static private SkinManager _instance;
    static public SkinManager Instance { get { return _instance; } }
    private void Awake()
    {
        _color = _colorSelector[_cIndex];
        if (_instance == null)
        {
            _instance  = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
        _renderer = _player.GetComponent<SpriteRenderer>();
        
      
    }
    public void ChangeSkin()
    {
        _player.GetComponent<ChangeColor>().ChangeColors(_color);

    }
    public void AvanzaColor()
    {
        if (_cIndex == _colorSelector.Length - 1)
        {
            _cIndex = 0;
        }
        else _cIndex++;

        _color = _colorSelector[_cIndex];
        ChangeSkin();
    }
    public void RetrocedeColor()
    {
        if (_cIndex == 0)
        {
            _cIndex = _colorSelector.Length - 1;
        }
        else _cIndex--;
        _color = _colorSelector[_cIndex];
        ChangeSkin();
    }
}
