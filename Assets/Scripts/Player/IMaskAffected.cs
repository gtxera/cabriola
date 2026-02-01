using UnityEngine;

public interface IMaskAffected
{
    void Affect(MaskState mask);

    void Leave();
}
