using System.Collections.Generic;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMask : ValidatedMonoBehaviour
{
    private static readonly int MaterialRadius = Shader.PropertyToID("_Radius");
    private static readonly int MaterialPosition = Shader.PropertyToID("_Position");
    private static readonly int MaterialLightColor = Shader.PropertyToID("_LightColor");

    [SerializeField]
    private InputActionReference _changeMaskAction;

    [SerializeField]
    private float _maskAreaBaseRadius;

    [SerializeField]
    private float _maskAreaGrowthPerGoat;
    
    [SerializeField, Self]
    private SphereCollider _maskArea;

    [SerializeField]
    private TweenSettings _radiusTweenSettings;

    [SerializeField]
    private TweenSettings _dimmingTweenSettings;
    
    [SerializeField]
    private TweenSettings _colorChangeTweenSettings;
    
    [SerializeField]
    private Material[] _lightedMaterials;

    [field: SerializeField]
    public Color GoatColor { get; private set; }
    
    [field: SerializeField]
    public Color PredatorColor { get; private set; }
    
    private MaskState _mask;
    
    private readonly List<IMaskAffected> _affecteds = new();

    private Tween _changeSizeTween;
    private Sequence _dimmingSequence;
    private Tween _colorChangeTween;
    
    public static PlayerMask Instance { get; private set; }

    public float MaskRadius => _maskArea.radius;
    public MaskState Mask => _mask;

    private void Awake()
    {
        if (Instance != null)
            Debug.LogError("PlayerMask already instantiated");

        Instance = this;
    }

    private void Start()
    {
        HerdController.Instance.GoatHerded += OnHerdedGoatsChanged;
        HerdController.Instance.GoatLost += OnHerdedGoatsChanged;

        GameManager.Instance.GameFinished += OnGameFinished;

        _maskArea.isTrigger = true;
        _maskArea.radius = _maskAreaBaseRadius;
        foreach (var material in _lightedMaterials)
        {
            material.SetFloat(MaterialRadius, _maskAreaBaseRadius);
            material.SetColor(MaterialLightColor, GoatColor);
        }
    }

    public void ChangeMask()
    {
        _mask = _mask == MaskState.Goat ? MaskState.Predator : MaskState.Goat;
        foreach (var maskAffected in _affecteds)
        {
            maskAffected.Affect(_mask);
        }
        _colorChangeTween.Stop();
        var from = _mask == MaskState.Goat ? PredatorColor : GoatColor;
        var to = _mask ==  MaskState.Goat ? GoatColor : PredatorColor;
        _colorChangeTween = Tween.Custom(from, to, _colorChangeTweenSettings, color =>
        {
            foreach (var material in _lightedMaterials)
            {
                material.SetColor(MaterialLightColor, color);
            }
        });
    }

    private void OnHerdedGoatsChanged(int goatCount)
    {
        _dimmingSequence.Stop();
        _changeSizeTween.Stop();

        var dimming = goatCount == 0;
        
        var targetRadius = _maskAreaBaseRadius + _maskAreaGrowthPerGoat * goatCount;
        _changeSizeTween = Tween.Custom(_maskArea.radius, targetRadius, _radiusTweenSettings,
            radius =>
            {
                _maskArea.radius = radius;
                foreach (var material in _lightedMaterials)
                {
                    material.SetFloat(MaterialRadius, radius);
                }
            });
        if (dimming)
        {
            _dimmingSequence = Sequence.Create(_changeSizeTween).Chain(
                Tween.Custom(_maskAreaBaseRadius, 0f, _dimmingTweenSettings,
                    radius =>
                    {
                        _maskArea.radius = radius;
                        foreach (var material in _lightedMaterials)
                        {
                            material.SetFloat(MaterialRadius, radius);
                        }
                    }));
            _dimmingSequence.OnComplete(GameManager.Instance.Lose);
        }
    }

    private void Update()
    {
        foreach (var material in _lightedMaterials)
        {
            material.SetVector(MaterialPosition, transform.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<IMaskAffected>(out var maskAffected))
            return;
        
        _affecteds.Add(maskAffected);
        maskAffected.Affect(_mask);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<IMaskAffected>(out var maskAffected))
            return;
        
        _affecteds.Remove(maskAffected);
        maskAffected.Leave();
    }
    
    private void OnGameFinished()
    {
        _changeMaskAction.action.Disable();
        _dimmingSequence.Stop();
    }
}

public enum MaskState
{
    Goat,
    Predator,
}