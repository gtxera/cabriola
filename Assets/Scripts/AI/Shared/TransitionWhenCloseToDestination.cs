using System;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

public class TransitionWhenCloseToDestination<TState> : Transition<TState>
{
    private readonly NavMeshAgent _navMeshAgent;
    private readonly float _distanceTolerance;
    public readonly Func<Vector3> _overrideDestination;
    
    public TransitionWhenCloseToDestination(TState from, TState to, NavMeshAgent navMeshAgent, Func<Vector3> overrideDestination = null, float distanceTolerance = 0.05f, Func<Transition<TState>, bool> condition = null, Action<Transition<TState>> onTransition = null, Action<Transition<TState>> afterTransition = null, bool forceInstantly = false) : base(from, to, condition, onTransition, afterTransition, forceInstantly)
    {
        _navMeshAgent = navMeshAgent;
        _overrideDestination = overrideDestination;
        _distanceTolerance = distanceTolerance;
    }

    public override bool ShouldTransition()
    {
        var isClose = _overrideDestination != null 
            ? Vector3.Distance(_navMeshAgent.transform.position, _overrideDestination()) <= _distanceTolerance
            : AiUtils.IsCloseToDestination(_navMeshAgent, _distanceTolerance);
        
        return base.ShouldTransition() && isClose;
    }
}
