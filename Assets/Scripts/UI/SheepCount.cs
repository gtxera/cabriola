using System;
using KBCore.Refs;
using TMPro;
using UnityEngine;

public class SheepCount : ValidatedMonoBehaviour
{
    [SerializeField, Self]
    private TextMeshProUGUI _text;

    private void Start()
    {
        GameManager.Instance.GameWin += () =>
            _text.SetText($"Você guiou {HerdController.Instance.HerdedGoatsCount} cabras ao seu destino");
    }
}
