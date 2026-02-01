using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

public static class RoamingState
{
    public static StateMachine<TState, RoamingStates, TEvents> Get<TState, TEvents>(
        NavMeshAgent navMeshAgent,
        Vector2 distanceRange,
        float movementSpeed,
        Vector2 idleDurationRange)
    {
        var roamingStateMachine = new StateMachine<TState, RoamingStates, TEvents>();
        var idleState = new IdleState();
        var movingState = new MovingState(navMeshAgent, distanceRange, movementSpeed);
        
        roamingStateMachine.AddState(RoamingStates.Idle, idleState);
        roamingStateMachine.AddState(RoamingStates.Moving, movingState);
        
        roamingStateMachine.AddTransition(new TransitionAfterDynamic<RoamingStates>(RoamingStates.Idle, RoamingStates.Moving, _ => Random.Range(idleDurationRange.x, idleDurationRange.y)));
        roamingStateMachine.AddTransition(new TransitionWhenReachedDestination<RoamingStates>(RoamingStates.Moving, RoamingStates.Idle, navMeshAgent));
        
        return roamingStateMachine;
    }
}
