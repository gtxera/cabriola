using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class Predator : ValidatedMonoBehaviour, IMaskAffected
{
    private static readonly int AnimatorKill = Animator.StringToHash("Kill");
    private static readonly int AnimationSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimationRunAway = Animator.StringToHash("RunAway");

    [SerializeField, Self]
    private NavMeshAgent _navMeshAgent;
    
    [SerializeField, Self]
    private Animator _animator;
    
    [SerializeField]
    private Transform _killPosition;
    
    [Header("Roaming"), SerializeField]
    private Vector2 _roamingDistanceRange;
    
    [SerializeField]
    private Vector2 _roamingIdleDurationRange;
    
    [SerializeField]
    private float _roamingMovementSpeed;

    [SerializeField]
    private AudioSource _movingAudio;

    [SerializeField]
    private AudioSource _killAudio;

    [Header("Alert"), SerializeField]
    private float _alertTimeToAttack = 3f;
    
    [Header("Approaching"), SerializeField]
    private float _approachingMovementSpeed = 3f;

    [SerializeField]
    private float _distanceToApproach;

    [Header("Attacking")]
    [Header("Preying"), SerializeField]
    private float _attackCooldown;
    
    [Header("Charging"), SerializeField]
    private float _chargingMovementSpeed;
    
    [Header("Killing"), SerializeField]
    private float _attackRange;

    [SerializeField]
    private float _scareGoatRadius;

    [SerializeField]
    private float _attackStayDuration;
    
    [Header("Escaping"), SerializeField]
    private float _escapeDistance;

    private StateMachine<PredatorStates, PredatorEvents> _stateMachine;

    private bool _inMaskArea;

    private Goat _currentTarget;

    private int _goatsDetected;
    
    private void Start()
    {
        _navMeshAgent.updateRotation = false;
        ConfigureStateMachine();

        GameManager.Instance.GameFinished += () => _stateMachine.Trigger(PredatorEvents.GameFinished);
    }

    private void ConfigureStateMachine()
    {
        _stateMachine = new StateMachine<PredatorStates, PredatorEvents>();
        
        var roamingState = RoamingState.Get<PredatorStates, PredatorEvents>(_navMeshAgent, _roamingDistanceRange, _roamingMovementSpeed, _roamingIdleDurationRange);
        _stateMachine.AddState(PredatorStates.Roaming, roamingState);
        
        _stateMachine.AddState(PredatorStates.Alert);

        var approachingState = new FollowTargetState<PredatorStates>(_navMeshAgent, _approachingMovementSpeed,
            () => HerdController.Instance.GetClosestPreyingAreaPosition(transform.position));
        _stateMachine.AddState(PredatorStates.Approaching, approachingState);

        var attackingState = ConfigureAttackStateMachine();
        _stateMachine.AddState(PredatorStates.Attacking, attackingState);

        var escapingState = new RunAwayState<PredatorStates>(_navMeshAgent, _approachingMovementSpeed,
            () => PlayerMask.Instance.MaskRadius + _escapeDistance,
            () => PlayerMask.Instance.transform,
            () => _inMaskArea && PlayerMask.Instance.Mask == MaskState.Predator,
            () => _animator.SetTrigger(AnimationRunAway),
            () => _animator.ResetTrigger(AnimationRunAway));
        _stateMachine.AddState(PredatorStates.Escaping, escapingState);
        
        var gameFinishedState = new State<PredatorStates>(onEnter: _ => gameObject.SetActive(false));
        _stateMachine.AddState(PredatorStates.GameFinished, gameFinishedState);
        
        _stateMachine.AddTriggerTransition(PredatorEvents.Alert, PredatorStates.Roaming, PredatorStates.Alert);
        
        _stateMachine.AddTriggerTransition(PredatorEvents.LoseInterest, PredatorStates.Alert, PredatorStates.Roaming);
        
        _stateMachine.AddTransition(new TransitionAfter<PredatorStates>(PredatorStates.Alert, PredatorStates.Approaching, _alertTimeToAttack, _ => HerdController.Instance.HerdedGoatsCount > 0));

        var approachingToAttackingTransition =
            new TransitionWhenCloseToDestination<PredatorStates>(PredatorStates.Approaching, PredatorStates.Attacking,
                _navMeshAgent, null, _distanceToApproach);
        _stateMachine.AddTransition(approachingToAttackingTransition);
        
        _stateMachine.AddTransition(PredatorStates.Attacking, PredatorStates.Roaming, _ => HerdController.Instance.HerdedGoatsCount == 0);
        
        _stateMachine.AddTriggerTransition(PredatorEvents.Escape, PredatorStates.Roaming, PredatorStates.Escaping);
        _stateMachine.AddTriggerTransition(PredatorEvents.Escape, PredatorStates.Alert, PredatorStates.Escaping);
        _stateMachine.AddTriggerTransition(PredatorEvents.Escape, PredatorStates.Approaching, PredatorStates.Escaping);
        _stateMachine.AddTriggerTransition(PredatorEvents.Escape, PredatorStates.Attacking, PredatorStates.Escaping);
        
        _stateMachine.AddTriggerTransitionFromAny(PredatorEvents.GameFinished, PredatorStates.GameFinished);

        var escapingToRoamingTransition =
            new TransitionWhenReachedDestination<PredatorStates>(PredatorStates.Escaping, PredatorStates.Roaming, _navMeshAgent);
        _stateMachine.AddTransition(escapingToRoamingTransition);
        
        _stateMachine.Init();
    }

    private StateBase<PredatorStates> ConfigureAttackStateMachine()
    {
        var stateMachine = new AttackStateMachine<PredatorStates, PredatorAttackStates, string>();

        var preyingState = new FollowTargetState<PredatorAttackStates>(_navMeshAgent, HerdController.Instance.HerdSpeed,
            () => HerdController.Instance.GetClosestPreyingAreaPosition(transform.position));
        stateMachine.AddState(PredatorAttackStates.Preying, preyingState);

        var chargingState =
            new FollowTargetState<PredatorAttackStates>(_navMeshAgent, _chargingMovementSpeed,
                () => _currentTarget.transform.position);
        stateMachine.AddState(PredatorAttackStates.Charging, chargingState);
        
        stateMachine.AddState(PredatorAttackStates.Killing, onEnter: _ => _animator.SetTrigger(AnimatorKill));
        
        stateMachine.AddTransition(new TransitionAfter<PredatorAttackStates>(PredatorAttackStates.Preying, PredatorAttackStates.Charging, _attackCooldown,
            onTransition: _ =>
            {
                _currentTarget = HerdController.Instance.GetClosestGoat(transform.position);
            }));
        stateMachine.AddTransition(new TransitionWhenCloseToDestination<PredatorAttackStates>(PredatorAttackStates.Charging, PredatorAttackStates.Killing, _navMeshAgent, null, _attackRange));
        stateMachine.AddTransition(new TransitionAfter<PredatorAttackStates>(PredatorAttackStates.Killing, PredatorAttackStates.Preying, _attackStayDuration));

        return stateMachine;
    }

    public void KillTarget()
    {
        _currentTarget.Kill(_killPosition.position);
        _currentTarget =  null;
        _animator.ResetTrigger(AnimatorKill);

        var colliders = Physics.OverlapSphere(transform.position, _scareGoatRadius, LayerMask.GetMask("Characters"));
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<Goat>(out var goat))
                goat.Scare();
        }
        _killAudio.Play();
    }

    private void Update()
    {
        _stateMachine.OnLogic();
        
        _animator.SetFloat(AnimationSpeed, _navMeshAgent.velocity.sqrMagnitude - 0.01f);
        
        if (_navMeshAgent.velocity.sqrMagnitude > 0.01f && !_movingAudio.isPlaying)
            _movingAudio.Play();
        else if (_navMeshAgent.velocity.sqrMagnitude < 0.01f && _movingAudio.isPlaying)
            _movingAudio.Stop();
    }

    public void DetectGoat()
    {
        _stateMachine.Trigger(PredatorEvents.Alert);
        _goatsDetected++;
    }

    public void LoseGoat()
    {
        _goatsDetected--;
        if (_goatsDetected == 0)
            _stateMachine.Trigger(PredatorEvents.LoseInterest);
    }

    public void Affect(MaskState mask)
    {
        _inMaskArea = true;
        
        switch (mask)
        {
            case MaskState.Goat:
                _stateMachine.Trigger(PredatorEvents.Alert);
                break;
            case MaskState.Predator:
                _stateMachine.Trigger(PredatorEvents.Escape);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mask), mask, null);
        }
    }

    public void Leave()
    {
        _inMaskArea = false;

        _stateMachine.Trigger(PredatorEvents.LoseInterest);
    }

    private class AttackStateMachine<TState, TInner, TEvents> : StateMachine<TState, TInner, TEvents>
    {
        public override void OnEnter()
        {
            SoundtrackManager.Instance.StartAttack();

            base.OnEnter();
        }

        public override void OnExit()
        {            
            SoundtrackManager.Instance.StopAttack();

            base.OnExit();
        }
    }
}
