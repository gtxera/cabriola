using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

public class RunAwayState<TState> : StateBase<TState>
{
    private readonly NavMeshAgent _navMeshAgent;
    private readonly float _movementSpeed;
    private readonly Func<float> _distanceToRunAwaySelector;
    private readonly Func<Transform> _transformToRunAwaySelector;
    private readonly Func<bool> _shouldRecalculateDestination;
    private readonly Action _onEnter;
    private readonly Action _onExit;
    
    public RunAwayState(
        NavMeshAgent navMeshAgent,
        float movementSpeed, Func<float> distanceToRunAwaySelector,
        Func<Transform> transformToRunAwaySelector,
        Func<bool> shouldRecalculateDestination, 
        Action onEnter = null,
        Action onExit = null) : base(false)
    {
        _navMeshAgent = navMeshAgent;
        _movementSpeed = movementSpeed;
        _distanceToRunAwaySelector = distanceToRunAwaySelector;
        _transformToRunAwaySelector = transformToRunAwaySelector;
        _shouldRecalculateDestination = shouldRecalculateDestination;
        _onEnter = onEnter;
        _onExit = onExit;
    }

    public override void OnEnter()
    {
        _navMeshAgent.speed = _movementSpeed;
        _navMeshAgent.SetDestination(CalculateDestination());
        _onEnter?.Invoke();
    }

    public override void OnExit()
    {
        _onExit?.Invoke();
    }

    public override void OnLogic()
    {
        if (!_shouldRecalculateDestination())
            return;

        var destination = CalculateDestination();
        _navMeshAgent.SetDestination(destination);
    }

    private Vector3 CalculateDestination()
    {
        var runningFromPosition = _transformToRunAwaySelector().position;
        var direction = (_navMeshAgent.transform.position - runningFromPosition).normalized;
        var distance = _distanceToRunAwaySelector();
        return runningFromPosition + direction * distance;
    }
}
