using UnityEngine;
using UnityEngine.AI;

public static class AiUtils
{
    public static bool TryGetValidReachableDestination(
        NavMeshAgent agent,
        Vector3 desiredPosition,
        float sampleRadius,
        out Vector3 validPosition)
    {
        validPosition = Vector3.zero;
        
        if (!NavMesh.SamplePosition(desiredPosition, out var hit, sampleRadius, agent.areaMask))
        {
            return false;
        }
        
        validPosition = hit.position;
        return true;
    }

    public static bool HasReachedDestination(NavMeshAgent agent, float extraTolerance = 0.05f)
    {
        if (agent.pathPending)
            return false;

        if (agent.remainingDistance > agent.stoppingDistance + extraTolerance)
            return false;

        if (!agent.hasPath)
            return true;
        
        return agent.velocity.sqrMagnitude < 0.01f;
    }

    public static bool IsCloseToDestination(NavMeshAgent agent, float extraTolerance = 0.05f)
    {
        if (agent.pathPending)
            return false;

        return agent.remainingDistance <= agent.stoppingDistance + extraTolerance;
    }
}
