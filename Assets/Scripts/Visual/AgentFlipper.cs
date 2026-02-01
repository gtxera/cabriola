using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.AI;

public class AgentFlipper : ValidatedMonoBehaviour
{
    [SerializeField, Self]
    private NavMeshAgent _agent;

    [SerializeField, Self]
    private SpriteRenderer _billboardFlipper;

    [SerializeField]
    private bool _inverted;

    private float _lastDirectionX;

    private void Update()
    {
        var velocity = _agent.velocity;
        if (velocity.x == 0)
            return;

        var velocityDirection = Mathf.Sign(velocity.x);
        if (!Mathf.Approximately(velocityDirection, _lastDirectionX))
        {
            _billboardFlipper.flipX = velocityDirection > 0 != _inverted;
            _lastDirectionX = velocityDirection;
        }
    }
}
