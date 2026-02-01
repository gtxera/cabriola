using System;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _prefab;
    
    [SerializeField]
    private float _spawnRadius;
    
    [SerializeField]
    private float _spawnAngle;

    [SerializeField]
    private float _spawnTime;

    [SerializeField]
    private Vector2 _spawnIntervalRange;

    [SerializeField]
    private int _spawnedOnStart;

    private Tween _spawnTimer;

    private void Start()
    {
        HerdController.Instance.BeganMoving += StartSpawnTimer;
        GameManager.Instance.GameFinished += Stop;
        
        var playerPosition = GameObject.FindWithTag("Player").transform.position;
        for (var i = 0; i < _spawnedOnStart; i++)
            Instantiate(_prefab, playerPosition + Vector3.back, Quaternion.identity);
            
    }

    private void StartSpawnTimer()
    {
        _spawnTimer = SetupTimer();
    }

    private Tween SetupTimer()
    {
        var timeToSpawn = Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
        var timer = Tween.Delay(timeToSpawn);
        timer.OnComplete(() =>
        {
            Spawn();
            _spawnTimer = SetupTimer();
        });
        return timer;
    }

    private void Spawn()
    {
        var canSpawn = HerdController.Instance.GetSpawnPosition(_spawnTime, _spawnRadius, out var position);
        if (canSpawn)
            Instantiate(_prefab, position, Quaternion.identity);
    }

    private void Stop()
    {
        _spawnTimer.Stop();
    }
}
