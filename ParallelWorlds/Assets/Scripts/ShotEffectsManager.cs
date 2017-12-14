using UnityEngine;
using System.Collections;

public class ShotEffectsManager : MonoBehaviour 
{
	[SerializeField] ParticleSystem muzzleFlash;
	[SerializeField] AudioSource gunAudio;
	[SerializeField] GameObject impactPrefab;

	ParticleSystem impactEffect;
	int universeLayer;

	//Create the impact effect for our shots
	public void Initialize()
	{
		if (impactPrefab != null) {
			impactEffect = Instantiate(impactPrefab).GetComponent<ParticleSystem>();
		}
		UpdateUniverseSettings();
	}

	//Play muzzle flash and audio
	public void PlayShotEffects()
	{
		if (muzzleFlash != null) {
			muzzleFlash.Stop (true);
			muzzleFlash.Play (true);
		}
		if (gunAudio != null) {
			gunAudio.Stop ();
			gunAudio.Play ();
		}
	}

	//Play impact effect and target position
	public void PlayImpactEffect(Vector3 impactPosition)
	{
		if (impactEffect != null) {
			impactEffect.transform.position = impactPosition;   
			impactEffect.Stop ();
			impactEffect.Play ();
		}
	}

	public void SetUniverseSettings(UniverseLayerSettings universe)
    {
        universeLayer = universe.layer;
		UpdateUniverseSettings();
    }

	private void UpdateUniverseSettings()
	{
		if (impactEffect != null)
		{
			impactEffect.gameObject.layer = universeLayer;
		}
	}
}