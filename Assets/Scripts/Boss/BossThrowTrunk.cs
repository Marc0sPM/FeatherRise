using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossThrowTrunk : MonoBehaviour
{
    [SerializeField] private GameObject _myTrunkPrefab;
    private Vector3 _trunkSpawnPoint;
    [SerializeField] private float _offset;
    private Transform _playerTransform;
    // Start is called before the first frame update
    public void SpawnTrunk()
    {
        _trunkSpawnPoint = _playerTransform.position;
        _trunkSpawnPoint.y = gameObject.transform.position.y + _offset;
        SoundComponent.Instance.PlaySound(SoundComponent.Instance._spawnTrunk);
        Instantiate(_myTrunkPrefab, _trunkSpawnPoint, Quaternion.identity);
    }
    void Start()
    {
        _playerTransform = GameManager.Instance.SetPlayer().transform;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
