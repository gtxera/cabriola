using System;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ChangeMaskButton : ValidatedMonoBehaviour
{
    [SerializeField, Self]
    private Button _button;

    [SerializeField, Self]
    private Image _mask;

    [SerializeField]
    private GameObject _goatMask;

    [SerializeField]
    private GameObject _predatorMask;

    private void Start()
    {
        _button.onClick.AddListener(ChangeMask);
        _goatMask.SetActive(true);
        _predatorMask.SetActive(false);
    }

    private void ChangeMask()
    {
        PlayerMask.Instance.ChangeMask();
        switch (PlayerMask.Instance.Mask)
        {
            case MaskState.Goat:
                _goatMask.SetActive(true);
                _predatorMask.SetActive(false);
                break;
            case MaskState.Predator:
                _predatorMask.SetActive(true);
                _goatMask.SetActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
