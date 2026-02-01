using System;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

public class FollowTargetState<TState> : StateBase<TState>
{
    private readonly NavMeshAgent _navMeshAgent;
    private readonly float _speed;
    private readonly Func<Vector3> _targetSelector;
    private readonly Action _onEnter;
    private readonly Action _onExit;
    
    public FollowTargetState(NavMeshAgent navMeshAgent, float speed, Func<Vector3> targetSelector, Action onEnter = null, Action onExit = null) : base(false)
    {
        _navMeshAgent = navMeshAgent;
        _speed = speed;
        _targetSelector = targetSelector;
        _onEnter = onEnter;
        _onExit = onExit;
    }

    public override void OnEnter()
    {
        _navMeshAgent.speed = _speed;
        _navMeshAgent.SetDestination(_targetSelector());
        _onEnter?.Invoke();
    }

    override public void OnExit()
    {
        _onExit?.Invoke();
    }

    public override void OnLogic()
    {
        var target = _targetSelector();
        _navMeshAgent.SetDestination(target);
    }
}
