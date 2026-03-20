using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementBackground : MonoBehaviour
{
    [SerializeField] private Vector2 _velocity;

    private Vector2 _offset;

    private Material _material;

    [SerializeField] private Rigidbody2D _rbplayer;

    // Start is called before the first frame update
    void Awake()
    {
        _material = GetComponent<SpriteRenderer>().material;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
    }

    // Update is called once per frame
    void Update()
    {
        _offset = (_rbplayer.velocity.x * 0.1f) * _velocity * Time.deltaTime; //Mirar la frenada más adelante
        _material.mainTextureOffset += _offset;
    }
}
