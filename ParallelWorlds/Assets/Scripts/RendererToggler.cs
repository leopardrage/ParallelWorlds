using UnityEngine;

public class RendererToggler : MonoBehaviour 
{
    [SerializeField] float turnOnDelay = .1f;
    [SerializeField] float turnOffDelay = 3.5f;
    [SerializeField] bool enabledOnLoad = false;

    Renderer[] renderers;

    void Awake () 
    {
        renderers = GetComponentsInChildren<Renderer> (true); 

        if (enabledOnLoad)
            EnableRenderers ();
        else
            DisableRenderers ();
    }

    //Method used by our Unity events to show and hide the player
    public void ToggleRenderersDelayed(bool isOn)
    {
        if (isOn)
            Invoke ("EnableRenderers", turnOnDelay);
        else
            Invoke ("DisableRenderers", turnOffDelay);
    }

    public void EnableRenderers()
    {
        for (int i = 0; i < renderers.Length; i++) 
        {
            renderers [i].enabled = true;
        }
    }

    public void DisableRenderers()
    {
        for (int i = 0; i < renderers.Length; i++) 
        {
            renderers [i].enabled = false;
        }
    }

    //Will be used to change the color of the players for different options
    public void ChangeColor(Color newColor)
    {
        for (int i = 0; i < renderers.Length; i++) 
        {
            renderers [i].material.color = newColor;
        }
    }
}