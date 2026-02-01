using System;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityHFSM;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class Goat : ValidatedMonoBehaviour, IMaskAffected
{
    private static readonly int AnimationDie = Animator.StringToHash("Die");
    private static readonly int AnimationRunAway = Animator.StringToHash("RunAway");
    private static readonly int AnimationSpeed = Animator.StringToHash("Speed");

    [SerializeField, Self]
    private NavMeshAgent _navMeshAgent;
    
    [SerializeField, Self]
    private Animator _animator;
    
    [SerializeField, Child]
    private GoatDetectionRange[] _goatDetectionRanges;

    [Header("Audio"), SerializeField, Child]
    private AudioSource _audioSource;

    [SerializeField]
    private AudioResource _scaredSound;

    [SerializeField]
    private AudioResource _dieSound;
    
    [SerializeField]
    private AudioResource _joiningSound;

    [SerializeField]
    private AudioResource _roamingSound;

    [SerializeField]
    private AudioSource _repeatingSound;
    
    [SerializeField]
    private bool _startHerded;
    
    [Header("Roaming"), SerializeField]
    private Vector2 _roamingDistanceRange;
    
    [SerializeField]
    private Vector2 _roamingIdleDurationRange;
    
    [SerializeField]
    private float _roamingMovementSpeed;

    [Header("Herded"), SerializeField]
    private float _additionalHerdedSpeed;

    [Header("Joining Herd"), SerializeField]
    private float _joiningHerdSpeed;

    [SerializeField]
    private float _distanceFromRallyToJoinHerd;

    [Header("Escaping"), SerializeField]
    private float _escapeDistance;
    
    [SerializeField]
    private float _escapeSpeed;
    
    private StateMachine<GoatStates, GoatEvents> _stateMachine;

    private bool _inMaskArea;

    private Vector3 _diePosition;

    private void Start()
    {
        _navMeshAgent.updateRotation = false;
        ConfigureStateMachine();
        GameManager.Instance.GameFinished += () => _stateMachine.Trigger(GoatEvents.GameFinished);
    }

    private void ConfigureStateMachine()
    {
        if (_stateMachine != null)
            return;
        
        _stateMachine = new StateMachine<GoatStates, GoatEvents>();
        
        var roamingState = RoamingState.Get<GoatStates, GoatEvents>(_navMeshAgent, _roamingDistanceRange, _roamingMovementSpeed, _roamingIdleDurationRange);
        _stateMachine.AddState(GoatStates.Roaming, roamingState);

        var joiningHerdState = new FollowTargetState<GoatStates>(_navMeshAgent, _joiningHerdSpeed, () => PlayerMask.Instance.transform.position,
            () =>
            {
                _audioSource.resource = _joiningSound;
                _audioSource.Play();
            });
        _stateMachine.AddState(GoatStates.JoiningHerd, joiningHerdState);
        
        var herdedState = new FollowTargetState<GoatStates>(_navMeshAgent, HerdController.Instance.HerdSpeed + _additionalHerdedSpeed,
            () => HerdController.Instance.transform.position,
            () =>
            {
                foreach (var detection in _goatDetectionRanges)
                    detection.DetectionEnabled = true;
            },
            () =>
            {
                foreach (var detection in _goatDetectionRanges)
                    detection.DetectionEnabled = false;
            });
        _stateMachine.AddState(GoatStates.Herded, herdedState);
        
        var escapingState = new RunAwayState<GoatStates>(_navMeshAgent, _escapeSpeed, 
            () => PlayerMask.Instance.MaskRadius + _escapeDistance,
            () => PlayerMask.Instance.transform,
            () => _inMaskArea && PlayerMask.Instance.Mask == MaskState.Predator,
            () =>
            {
                _animator.SetBool(AnimationRunAway, true);
                _audioSource.resource = _scaredSound;
                _audioSource.Play();
            },
            () => _animator.SetBool(AnimationRunAway, false));
        _stateMachine.AddState(GoatStates.Escaping, escapingState);

        var deadState = new State<GoatStates>(onEnter: _ => Die());
        _stateMachine.AddState(GoatStates.Dead, deadState);

        var gameFinishedState = new State<GoatStates>(onEnter: _ => _navMeshAgent.ResetPath());
        _stateMachine.AddState(GoatStates.GameFinished, gameFinishedState);
        
        _stateMachine.AddTriggerTransitionFromAny(GoatEvents.Kill, GoatStates.Dead, forceInstantly: true);
        
        _stateMachine.AddTriggerTransition(GoatEvents.Join, GoatStates.Roaming, GoatStates.JoiningHerd);

        var joiningToHerdedTransition = new TransitionWhenCloseToDestination<GoatStates>(GoatStates.JoiningHerd,
            GoatStates.Herded, _navMeshAgent, () => HerdController.Instance.RallyPoint.position, _distanceFromRallyToJoinHerd, onTransition: _ => HerdController.Instance.Herd(this));
        _stateMachine.AddTransition(joiningToHerdedTransition);
        _stateMachine.AddTriggerTransition(GoatEvents.Herd, GoatStates.JoiningHerd, GoatStates.Herded, onTransition: _ => HerdController.Instance.Herd(this));
        
        _stateMachine.AddTriggerTransition(GoatEvents.Escape, GoatStates.Roaming, GoatStates.Escaping);
        _stateMachine.AddTriggerTransition(GoatEvents.Escape, GoatStates.JoiningHerd, GoatStates.Escaping);
        _stateMachine.AddTriggerTransition(GoatEvents.Escape, GoatStates.Herded, GoatStates.Escaping, 
            onTransition: _ => HerdController.Instance.Lose(this));

        var escapingToRoamingTransition =
            new TransitionWhenReachedDestination<GoatStates>(GoatStates.Escaping, GoatStates.Roaming, _navMeshAgent);
        _stateMachine.AddTransition(escapingToRoamingTransition);
        
        _stateMachine.AddTriggerTransitionFromAny(GoatEvents.GameFinished, GoatStates.GameFinished);

        if (_startHerded)
        {
            _stateMachine.SetStartState(GoatStates.Herded);
            HerdController.Instance.Herd(this);
        }
        
        _stateMachine.Init();
    }

    private void Die()
    {
        _navMeshAgent.ResetPath();
        _navMeshAgent.enabled = false;
        transform.position = _diePosition;
        HerdController.Instance.Lose(this);
        _animator.SetTrigger(AnimationDie);
        _audioSource.resource = _dieSound;
        _audioSource.Play();
        _repeatingSound.Stop();
    }

    private void Update()
    {
        _stateMachine.OnLogic();
        
        _animator.SetFloat(AnimationSpeed, _navMeshAgent.velocity.sqrMagnitude - 0.01f);
        Debug.Log(_stateMachine.GetActiveHierarchyPath());
    }

    public void Kill(Vector3 diePosition)
    {
        _diePosition = diePosition;
        _stateMachine.Trigger(GoatEvents.Kill);
    }

    public void Scare()
    {
        _stateMachine.Trigger(GoatEvents.Escape);
    }

    public void Herd()
    {
        _stateMachine.Trigger(GoatEvents.Herd);
    }

    public void Affect(MaskState mask)
    {
        _inMaskArea = true;
        ConfigureStateMachine();
        switch (mask)
        {
            case MaskState.Goat:
                _stateMachine.Trigger(GoatEvents.Join);
                break;
            case MaskState.Predator:
                _stateMachine.Trigger(GoatEvents.Escape);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mask), mask, null);
        }
    }

    public void Leave()
    {
        _inMaskArea = false;
    }
}
