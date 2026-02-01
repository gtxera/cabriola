using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerMovement : ValidatedMonoBehaviour
{
    private static readonly int AnimationSpeed = Animator.StringToHash("Speed");

    [SerializeField]
    private InputActionReference _moveAction;

    [SerializeField]
    private float _movementSpeed;

    [SerializeField]
    private float _acceleration;
    
    [SerializeField]
    private float _deceleration;

    [SerializeField]
    private AudioSource _movingAudio;

    [SerializeField]
    private AudioSource _stopAudio;
    
    [SerializeField, Self]
    private CharacterController _characterController;

    [SerializeField]
    private Transform _vfx;

    [SerializeField]
    private Vector2 _vfxPositions;
    
    [SerializeField, Self]
    private Animator _animator;
    
    [SerializeField, Self]
    private SpriteRenderer _spriteRenderer;
    
    private Vector3 _movementInput;
    private float _currentVelocity;

    private void Start()
    {
        _moveAction.action.Enable();
        _moveAction.action.performed += OnMovePerformed;
        _moveAction.action.canceled += OnMoveCanceled;
        
        GameManager.Instance.GameFinished += OnGameFinished;
    }

    private void Update()
    {
        if (_movementInput != Vector3.zero)
            _currentVelocity = Mathf.Min(_currentVelocity + _acceleration * Time.deltaTime, _movementSpeed);
        else
            _currentVelocity = Mathf.Max(_currentVelocity - _deceleration * Time.deltaTime, 0);
        
        _characterController.SimpleMove(_currentVelocity * _movementInput);
        _animator.SetFloat(AnimationSpeed, _currentVelocity - 0.001f);
    }
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        var input = context.ReadValue<Vector2>().normalized;
        _movementInput = new Vector3(input.x, 0, input.y);

        if (input.x != 0)
        {
            var flipped = input.x > 0;
            _spriteRenderer.flipX = input.x > 0;

            var position = _vfx.transform.localPosition;
            position.x = (flipped ? _vfxPositions.y : _vfxPositions.x);
            _vfx.transform.localPosition = position;
        }
        _movingAudio.Play();
        _stopAudio.Stop();
    }

    private void OnMoveCanceled(InputAction.CallbackContext _)
    {
        _movementInput = Vector3.zero;
        _movingAudio.Stop();
        _stopAudio.Play();
    }

    private void OnGameFinished()
    {
        enabled = false;
        _animator.SetFloat(AnimationSpeed, -1f);
        _moveAction.action.Disable();
    }
}
