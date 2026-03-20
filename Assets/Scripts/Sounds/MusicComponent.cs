using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicComponent : MonoBehaviour
{
    #region Accessors
    #endregion

    #region Paramenters
    [SerializeField]
    private AudioSource _audioSourceOST1;
    [SerializeField]
    private AudioSource _audioSourceOST2;
    [SerializeField]
    private AudioSource _audioSourceOST3;
    /// <summary>
    /// tiempo de la transicion
    /// </summary>
    [SerializeField]
    private float _fadeTime;
    /// <summary>
    /// umbral para que la transicion se complete rapidamente 
    /// </summary>
    [SerializeField]
    private float _umbralVolume;
    [SerializeField]
    private float _maxVolume;
    #endregion

    #region Properties
    [SerializeField] private bool _onOST1;
    [SerializeField] private bool _onOST2;
    [SerializeField] private bool _onOST3;

    public bool _playOST1;
    public bool _playOST2;
    public bool _playOST3;
    #endregion

    #region Methods
    /// <summary>
    /// cambia la pista de audio a la indicada, progresivamente bajando las otras pistas
    /// </summary>
    /// 
    private void ChangeMusic(AudioSource currentSource)
    {
        if (currentSource.volume <= _umbralVolume)
        {
            if (!_onOST1)
            {
                _audioSourceOST1.volume = Mathf.Lerp(_audioSourceOST1.volume, 0, Time.deltaTime * _fadeTime);
            }
            if (!_onOST2)
            {
                _audioSourceOST2.volume = Mathf.Lerp(_audioSourceOST2.volume, 0, Time.deltaTime * _fadeTime);
            }
            if (!_onOST3)
            {
                _audioSourceOST3.volume = Mathf.Lerp(_audioSourceOST3.volume, 0, Time.deltaTime * _fadeTime);
            }

            currentSource.volume = Mathf.Lerp(currentSource.volume, 1, Time.deltaTime * _fadeTime);
        }
        else
        {
            if (!_onOST1)
            {
                _audioSourceOST1.volume = 0;
            }
            if (!_onOST2)
            {
                _audioSourceOST2.volume = 0;
            }
            if (!_onOST3)
            {
                _audioSourceOST3.volume = 0;
            }

            currentSource.volume = 1;
        }

    }
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        _audioSourceOST1 = transform.GetChild(0).GetComponent<AudioSource>();       // AudioSource OST1
        _audioSourceOST2 = transform.GetChild(1).GetComponent<AudioSource>();       // AudioSource OST2
        _audioSourceOST3 = transform.GetChild(2).GetComponent<AudioSource>();       // AudioSource OST3
        ChangeMusic(_audioSourceOST1);
    }

    private void ChangeState(ref bool myState)
    {
        _onOST1 = false;
        _onOST2 = false;
        _onOST3 = false;
        myState = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (_playOST1)
        {
            ChangeState(ref _onOST1);
            _playOST1 = false;
            _playOST2 = false;
            _playOST3 = false;
        }
        if (_playOST2)
        {
            ChangeState(ref _onOST2);
            _playOST1 = false;
            _playOST2 = false;
            _playOST3 = false;
        }
        if (_playOST3)
        {
            ChangeState(ref _onOST3);
            _playOST1 = false;
            _playOST2 = false;
            _playOST3 = false;
        }

        if (_onOST1) ChangeMusic(_audioSourceOST1);
        if (_onOST2) ChangeMusic(_audioSourceOST2);
        if (_onOST3) ChangeMusic(_audioSourceOST3);
    }
}