using UnityEngine;

public class SwapEffectRemote : MonoBehaviour
{
    private Renderer[] _renderers;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    //Will be used to change the color of the players for different options
    public void OnSwapTransitionUpdate(float t, float transitionTime, UniverseState universeState)
    {
        // Check needed in case it is called before Awake by another script's
        // Awake or OnEnable method (because Unity call Awake, OnEnable and then
        // go for another script...)
        if (_renderers != null)
        {
            if (PlayerUniverse.localPlayerUniverse != null)
            {
                float progress = 0.0f;
                if (universeState.transitionState == UniverseState.TransitionState.Normal)
                {
                    progress = 1.0f;
                }
                else
                {
                    bool fadeIn = false;
                    if (universeState.transitionState == UniverseState.TransitionState.SwapOut)
                    {
                        fadeIn = (universeState.universe != PlayerUniverse.localPlayerUniverse.universeState.universe);
                    } 
                    else if (universeState.transitionState == UniverseState.TransitionState.SwapIn)
                    {
                        fadeIn = (universeState.universe == PlayerUniverse.localPlayerUniverse.universeState.universe);
                    }
                    progress = (fadeIn) ? t / transitionTime : (transitionTime - t) / transitionTime;
                }

                for (int i = 0; i < _renderers.Length; i++)
                {
                    foreach (Material material in _renderers[i].materials)
                    {
                        material.SetFloat("_Progress", progress);
                    };
                }
            }
        }
    }
}