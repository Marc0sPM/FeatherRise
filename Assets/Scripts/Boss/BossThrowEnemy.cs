using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BossThrowEnemy : MonoBehaviour
{
    [SerializeField] private GameObject _myEnemyPrefab;
    private Transform _enemySpawnPoint;
    [SerializeField] private Transform[] _spawnList; 
    // Start is called before the first frame update
    public void SpawnEnemy()
    {
        _enemySpawnPoint = _spawnList[Random.Range(0, _spawnList.Length)];
        SoundComponent.Instance.PlaySound(SoundComponent.Instance._bossEneymThrow);
        GameObject go = Instantiate(_myEnemyPrefab, _enemySpawnPoint.position, Quaternion.identity);
        go.GetComponentInChildren<ThownEnemyComponent>().SetSpin(this.transform.localScale.x);
    }
    
}
