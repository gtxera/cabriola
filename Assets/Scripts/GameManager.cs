using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action GameFinished = delegate { };
    public event Action GameWin = delegate { };
    public event Action GameOver = delegate { };
    
    private void Awake()
    {
        if (Instance != null)
            Debug.LogError("More than one game manager found");

        Instance = this;
    }

    public void Win()
    {
        GameFinished();
        GameWin();
        Debug.Log("Game win");
    }

    public void Lose()
    {
        GameFinished();
        GameOver();
        Debug.Log("Game over");
    }
}
