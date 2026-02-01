using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

public class MovingState : StateBase<RoamingStates>
{
    private readonly NavMeshAgent _navMeshAgent;
    private readonly Vector2 _distanceRange;
    private readonly float _movementSpeed;
    
    public MovingState(NavMeshAgent navMeshAgent, Vector2 distanceRange, float movementSpeed) : base(false)
    {
        _navMeshAgent = navMeshAgent;
        _distanceRange = distanceRange;
        _movementSpeed = movementSpeed;
    }

    public override void OnEnter()
    {
        bool hasPath;
        Vector3 destination;
        var currentPosition = _navMeshAgent.transform.position;
        do
        {
            var distance = Random.Range(_distanceRange.x, _distanceRange.y);
            var direction = Quaternion.AngleAxis(Random.Range(0, 360f), Vector3.up) * Vector3.right;
            var desiredPosition = currentPosition + direction * distance;
            hasPath = AiUtils.TryGetValidReachableDestination(_navMeshAgent, desiredPosition, 5f, out destination);
        } while (!hasPath && (destination - currentPosition).sqrMagnitude > _distanceRange.x * _distanceRange.x);
        
        _navMeshAgent.speed = _movementSpeed;
        _navMeshAgent.SetDestination(destination);
    }

    public override void OnExit()
    {
        _navMeshAgent.ResetPath();
    }
}
