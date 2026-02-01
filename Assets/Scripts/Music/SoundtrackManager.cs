using System;
using PrimeTween;
using UnityEngine;

public class SoundtrackManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource _normalMusic;
    
    [SerializeField]
    private AudioSource _predatorMusic;
    
    public static SoundtrackManager Instance {get; private set;}

    private int _attackCount;

    private Tween _from;
    private Tween _to;
    
    private void Awake()
    {
        Instance = this;
        _normalMusic.Play();
        _predatorMusic.Play();
        _predatorMusic.volume = 0f;
    }

    public void StartAttack()
    {
        if (_attackCount == 0)
            CrossFade(_normalMusic, _predatorMusic);
        
        _attackCount++;
    }

    public void StopAttack()
    {
        _attackCount--;
        
        if (_attackCount == 0)
            CrossFade(_predatorMusic, _normalMusic);
    }

    private void CrossFade(AudioSource from, AudioSource to)
    {
        _from.Stop();
        _to.Stop();
        
        _from = Tween.AudioVolume(from, 0f, 1f);
        _to = Tween.AudioVolume(to, 1f, 1f);
    }
}
