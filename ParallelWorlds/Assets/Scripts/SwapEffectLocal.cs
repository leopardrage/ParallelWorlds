using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SwapEffectLocal : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Vignette _vignette;
    [SerializeField] private AnimationCurve _innerVignette;
    [SerializeField] private AnimationCurve _outerVignette;
    [SerializeField] private AnimationCurve _saturation;
    [SerializeField] private AnimationCurve _fov;
    [SerializeField] private AudioClip _swapAudioClip;

    private AudioSource _audio;

    private void Reset()
    {
        _camera = GetComponent<Camera>();
    }

    private void Awake() 
    {
        _audio = GetComponent<AudioSource>();
    }

    // ------------- Universe Transition Local Effects ---------------

    public void OnSwapTransitionUpdate(float t, float transitionTime, UniverseState universeState)
    {
        float progress = t / transitionTime;

        // If it's starting to swap out, play sound effect
        if (universeState.transitionState == UniverseState.TransitionState.SwapOut && progress < float.Epsilon)
        {
            _audio.PlayOneShot(_swapAudioClip);
        }

        if (_camera != null)
        {
            _camera.fieldOfView = _fov.Evaluate(progress);
        }
        if (_vignette != null)
        {
            _vignette.minRadius = _innerVignette.Evaluate(progress);
            _vignette.maxRadius = _outerVignette.Evaluate(progress);
            _vignette.saturation = _saturation.Evaluate(progress);
        }
    }
}