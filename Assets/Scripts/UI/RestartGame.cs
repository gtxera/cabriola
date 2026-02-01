using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RestartGame : ValidatedMonoBehaviour
{
    [SerializeField, Self]
    private Button _button;

    private void Start()
    {
        _button.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
    }
}
