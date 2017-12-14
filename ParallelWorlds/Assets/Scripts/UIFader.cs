using UnityEngine;
using System.Collections;

// This class is used to fade in and out groups of UI
// elements.  It contains a variety of functions for
// fading in different ways.
[RequireComponent(typeof(CanvasGroup))]
public class UIFader : MonoBehaviour
{
    [SerializeField] private float _fadeSpeed = 1f;              // The amount the alpha of the UI elements changes per second.
    [SerializeField] private CanvasGroup _groupToFade;           // All the groups of UI elements that will fade in and out.
    [SerializeField] private bool _startVisible;                 // Should the UI elements be visible to start?
    [SerializeField] private bool _startWithFade;                // Should the UI elements begin fading with they start up? Fading can either be in or out (opposite of their starting alpha)

    private bool _visible;           // Whether the UI elements are currently visible.

    private void Reset()
    {
        //Attempt to grab the CanvasGroup on this object
        _groupToFade = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        //If the object should start visible, set it to be visible. Otherwise, set it invisible
        if (_startVisible)
        {
            SetVisible();
        }
        else
        {
            SetInvisible();
        }

        //If there shouldn't be any initial fade, leave this method
        if (!_startWithFade)
        {
            return;
        }

        //If the object is currently visible, fade out. Otherwise fade in
        if (_visible)
        {
            StartFadeOut();
        }
        else
        {
            StartFadeIn();
        }
    }

    //Publicly accessible methods for fading in or fading out without needing to start a 
    //coroutine. These are needed in order for UI events (like buttons) to start a fade in
    //or out.
    public void StartFadeIn()
    {
        StopFlashCoroutines();
        StartCoroutine("FadeIn");
    }

    public void StartFadeOut()
    {
        StopFlashCoroutines();
        StartCoroutine("FadeOut");
    }

    // These functions are used if fades are required to be instant.
    public void SetVisible()
    {
        _groupToFade.alpha = 1f;
        _visible = true;
    }


    public void SetInvisible()
    {
        _groupToFade.alpha = 0f;
        _visible = false;
    }

    private IEnumerator FadeIn()
    {
        // Fading needs to continue until the group is completely faded in
        while (_groupToFade.alpha < 1f)
        {
            //Increase the alpha
            _groupToFade.alpha += _fadeSpeed * Time.deltaTime;
            //Wait a frame
            yield return null;
        }

        // Since everthing has faded in now, it is visible.
        _visible = true;
    }

    private IEnumerator FadeOut()
    {
        while (_groupToFade.alpha > 0f)
        {
            _groupToFade.alpha -= _fadeSpeed * Time.deltaTime;

            yield return null;
        }

        _visible = false;
    }

    public void Flash()
    {
        StopFlashCoroutines();
        StartCoroutine("ProcessFlash");
    }

    private IEnumerator ProcessFlash()
    {
        yield return StartCoroutine("FadeIn");
        yield return StartCoroutine("FadeOut");
    }

    private void StopFlashCoroutines()
    {
        StopCoroutine("ProcessFlash");
        StopCoroutine("FadeIn");
        StopCoroutine("FadeOut");
    }
}