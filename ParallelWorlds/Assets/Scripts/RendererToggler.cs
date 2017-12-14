using UnityEngine;

public class RendererToggler : MonoBehaviour
{
    [SerializeField] private float _turnOnDelay = .1f;
    [SerializeField] private float _turnOffDelay = 3.5f;
    [SerializeField] private bool _enabledOnLoad = false;

    private Renderer[] _renderers;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);

        if (_enabledOnLoad)
        {
            EnableRenderers();
        }
        else
        {
            DisableRenderers();
        }
    }

    //Method used by our Unity events to show and hide the player
    public void ToggleRenderersDelayed(bool isOn)
    {
        if (isOn)
        {
            Invoke("EnableRenderers", _turnOnDelay);
        }
        else
        {
            Invoke("DisableRenderers", _turnOffDelay);
        }
    }

    public void EnableRenderers()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].enabled = true;
        }
    }

    public void DisableRenderers()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].enabled = false;
        }
    }

    //Will be used to change the color of the players for different options
    public void ChangeColor(Color newColor)
    {
        // Check needed in case it is called before Awake by another script's
        // Awake or OnEnable method (because Unity call Awake, OnEnable and then
        // go for another script...)
        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.color = newColor;
            }
        }
    }
}