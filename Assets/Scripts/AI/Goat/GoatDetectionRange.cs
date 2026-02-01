using System;
using UnityEngine;

public class GoatDetectionRange : MonoBehaviour
{
    [SerializeField]
    private bool _predator;
    
    public bool DetectionEnabled { get; set; }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!DetectionEnabled)
            return;
        
        switch (_predator)
        {
            case true when other.TryGetComponent<Predator>(out var predator):
                predator.DetectGoat();
                break;
            case false when other.TryGetComponent<Goat>(out var goat):
                goat.Herd();
                break;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (DetectionEnabled && _predator && other.TryGetComponent<Predator>(out var predator))
            predator.DetectGoat();
    }

    private void OnTriggerExit(Collider other)
    {
        if (DetectionEnabled && other.TryGetComponent<Predator>(out var predator))
            predator.LoseGoat();
    }
}
