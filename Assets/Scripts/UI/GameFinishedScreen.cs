using System;
using KBCore.Refs;
using UnityEngine;

public class GameFinishedScreen : ValidatedMonoBehaviour
{
    [SerializeField, Child(Flag.ExcludeSelf)]
    private RectTransform _root;

    [SerializeField]
    private bool _gameOver;

    private void Start()
    {
        if (_gameOver)
            GameManager.Instance.GameOver += ShowScreen;
        else
            GameManager.Instance.GameWin += ShowScreen;
    }

    private void ShowScreen()
    {
        _root.gameObject.SetActive(true);
    }
}
