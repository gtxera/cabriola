using System;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

public class TransitionWhenReachedDestination<TState> : Transition<TState>
{
    private readonly NavMeshAgent _navMeshAgent;
    private readonly float _distanceTolerance;
    
    public TransitionWhenReachedDestination(TState from, TState to, NavMeshAgent navMeshAgent, float distanceTolerance = 0.05f, Func<Transition<TState>, bool> condition = null, Action<Transition<TState>> onTransition = null, Action<Transition<TState>> afterTransition = null, bool forceInstantly = false) : base(from, to, condition, onTransition, afterTransition, forceInstantly)
    {
        _navMeshAgent = navMeshAgent;
        _distanceTolerance = distanceTolerance;
    }

    public override bool ShouldTransition()
    {
        return base.ShouldTransition() && AiUtils.HasReachedDestination(_navMeshAgent, _distanceTolerance);
    }
}
