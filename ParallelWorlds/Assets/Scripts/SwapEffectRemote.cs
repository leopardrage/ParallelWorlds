using UnityEngine;

public class SwapEffectRemote : MonoBehaviour
{
    private Renderer[] _renderers;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    //Will be used to change the color of the players for different options
    public void ApplyEffect(float t, bool reverse)
    {
        // Check needed in case it is called before Awake by another script's
        // Awake or OnEnable method (because Unity call Awake, OnEnable and then
        // go for another script...)
        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                foreach (Material material in _renderers[i].materials)
                {
                    if (reverse)
                    {
                        material.SetFloat("_Progress", 1.0f - t);
                    }
                    else
                    {
                        material.SetFloat("_Progress", t);
                    }
                };
            }
        }
    }
}