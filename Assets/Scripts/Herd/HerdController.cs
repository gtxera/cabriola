using System;
using System.Collections.Generic;
using System.Linq;
using KBCore.Refs;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

public class HerdController : ValidatedMonoBehaviour, IMaskAffected
{
    [SerializeField, Self]
    private SplineAnimate _herdPathAnimation;
    
    [SerializeField]
    private Transform _rallyPoint;

    [SerializeField]
    private Transform[] _preyingAreas;
    
    [SerializeField]
    private CinemachineCamera _herdCamera;
    
    //TODO: spline.EvaluatePosition para obter uma posicao a frente para spawnar

    [SerializeField]
    private List<Goat> _herdedGoats = new();

    private bool _started;
    
    public static HerdController Instance { get; private set; }

    public float HerdSpeed => _herdPathAnimation.MaxSpeed;
    public Transform RallyPoint => _rallyPoint;
    public int HerdedGoatsCount => _herdedGoats.Count;
    
    public event Action BeganMoving = delegate { };
    public event Action<int> GoatHerded = delegate { };
    public event Action<int> GoatLost = delegate { };

    private void Awake()
    {
        if (Instance != null)
            Debug.LogError("More than one HerdController instance found");

        Instance = this;
        transform.position = _herdPathAnimation.Container.transform.position;
    }

    private void Start()
    {
        _herdPathAnimation.Completed += GameManager.Instance.Win;
        GameManager.Instance.GameFinished += _herdPathAnimation.Pause;
    }

    public void Herd(Goat goat)
    {
        if (HerdedGoatsCount == 0 && _started)
            _herdPathAnimation.Play();
        
        _herdedGoats.Add(goat);
        GoatHerded(_herdedGoats.Count);
    }

    public void Lose(Goat goat)
    {
        _herdedGoats.Remove(goat);
        GoatLost(_herdedGoats.Count);
        
        if (HerdedGoatsCount == 0)
            _herdPathAnimation.Pause();
    }

    public Goat GetClosestGoat(Vector3 position)
    {
        var closestGoat = _herdedGoats[0];
        var closestGoatPosition = closestGoat.transform.position;
        foreach (var goat in _herdedGoats.Skip(1))
        {
            if ((position - goat.transform.position).sqrMagnitude < (position - closestGoatPosition).sqrMagnitude)
                closestGoat = goat;
        }

        return closestGoat;
    }

    public Vector3 GetClosestPreyingAreaPosition(Vector3 position)
    {
        var closestArea = _preyingAreas[0];
        foreach (var preyingArea in _preyingAreas.Skip(1))
        {
            if ((position - preyingArea.position).sqrMagnitude < (position - closestArea.position).sqrMagnitude)
                closestArea = preyingArea;
        }

        return closestArea.position;
    }

    public bool GetSpawnPosition(float timeInFuture, float spawnRadius, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        var normalizedDelta = timeInFuture / _herdPathAnimation.Duration;
        var normalizedFutureTime = _herdPathAnimation.NormalizedTime + normalizedDelta;
        if (!_herdPathAnimation.Container.Evaluate(normalizedFutureTime, out var position, out var tangent, out _))
            return false;

        spawnPosition = (Vector3)position + Random.insideUnitSphere * spawnRadius;
        spawnPosition.y = position.y;
        
        return true;
    }

    public void Affect(MaskState mask)
    {
        if (!_herdPathAnimation.IsPlaying && !_started)
        {
            _herdPathAnimation.Play();
            BeganMoving();
            _started = true;
        }
    }

    public void Leave() { }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _herdCamera.enabled = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _herdCamera.enabled = false;
    }
}
