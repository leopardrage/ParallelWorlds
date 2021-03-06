﻿using UnityEngine;
using System.Collections;

public class ShotEffectsManager : MonoBehaviour, IUniverseObserver
{
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private AudioSource _gunAudio;
    [SerializeField] private GameObject _impactPrefab;

    private ParticleSystem _impactEffect;
    private int _universeLayer;
    Universe _universe = Universe.UniverseUndefined;

    //Create the impact effect for our shots
    public void Initialize()
    {
        if (_impactPrefab != null)
        {
            _impactEffect = Instantiate(_impactPrefab).GetComponent<ParticleSystem>();
        }
        UpdateUniverseSettings();
    }

    //Play muzzle flash and audio
    public void PlayShotEffects()
    {
        if (_muzzleFlash != null)
        {
            _muzzleFlash.Stop(true);
            _muzzleFlash.Play(true);
        }
        if (_gunAudio != null)
        {
            // Play audio only if in the same universe of the local player
            if (PlayerUniverse.localPlayerUniverse != null && PlayerUniverse.localPlayerUniverse.universeState.universe == _universe)
            {
                _gunAudio.Stop();
                _gunAudio.Play();
            }
        }
    }

    //Play impact effect and target position
    public void PlayImpactEffect(Vector3 impactPosition)
    {
        if (_impactEffect != null)
        {
            _impactEffect.transform.position = impactPosition;
            _impactEffect.Stop();
            _impactEffect.Play();
        }
    }

    // ------------- IUniverseObserver ---------------

    public void SetUniverseSettings(UniverseLayerSettings universe)
    {
        _universeLayer = universe.layer;
        _universe = universe.universe;
        UpdateUniverseSettings();
    }

    private void UpdateUniverseSettings()
    {
        if (_impactEffect != null)
        {
            _impactEffect.gameObject.layer = _universeLayer;
        }
    }
}