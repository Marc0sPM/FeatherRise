using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundComponent : MonoBehaviour
{
    private static SoundComponent _instance;
    public static SoundComponent Instance { get { return _instance; } }


    private AudioSource _myAudioSource;


    public AudioClip _chestOpening;
    public AudioClip _itemRecolectable;
    public AudioClip _checkPoint;
    public AudioClip _dash;
    public AudioClip _jump;
    public AudioClip _playerAttack;
    public AudioClip _playerAirAttack;
    public AudioClip _featherThrow;
    public AudioClip _featherPlatform;
    public AudioClip _featherReturn;
    public AudioClip _playerTakesDamage;
    public AudioClip _pickItem;
    public AudioClip _button;
    public AudioClip _enemyHit;
    public AudioClip _respawn;
    public AudioClip _trunkHit;
    public AudioClip _bossAttack;
    public AudioClip _spawnTrunk;
    public AudioClip _bossTeleport;
    public AudioClip _bossEneymThrow;
    public AudioClip _bossReceiveDamage;


    public void PlaySound(AudioClip myAudio)
    {
        _myAudioSource.PlayOneShot(myAudio);
    }

    private void Awake()
    {
        _instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        _myAudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
