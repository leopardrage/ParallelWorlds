using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerShooting : NetworkBehaviour 
{
	[SerializeField] float shotCooldown = .3f;
	[SerializeField] Transform firePosition;
	[SerializeField] ShotEffectsManager shotEffects;

	[SyncVar (hook = "OnScoreChanged")] int score;

	float ellapsedTime;
	bool canShoot;

	LayerMask shootMask;

	void Start ()
	{
		shotEffects.Initialize ();

		if (isLocalPlayer)
		{
			canShoot = true;
		}
	}

	[ServerCallback]
	void OnEnable()
	{
		score = 0;
	}

	void Update()
	{
		if (!canShoot)
		{
			return;
		}

		ellapsedTime += Time.deltaTime;

		if (Input.GetButtonDown ("Fire1") && ellapsedTime > shotCooldown) 
		{
			ellapsedTime = 0;

			// Note, we pass these parameters because they are localPlayer's. Inside CmdFireShot method we are on the server and those data
			// will be of the version of the player on the server, that is not the one we are interested in.
			CmdFireShot (firePosition.position, firePosition.forward);
		}
	}

	[Command]
	void CmdFireShot(Vector3 origin, Vector3 direction)
	{
		RaycastHit hit;

		Ray ray = new Ray (origin, direction);
		Debug.DrawRay (ray.origin, ray.direction * 3f, Color.red, 1f);
		
		bool result = Physics.Raycast (ray, out hit, 50f, shootMask);

		if (result) 
		{
			PlayerHealth enemy = hit.transform.GetComponent<PlayerHealth> ();
			if (enemy != null) 
			{
				bool wasKillShot = enemy.TakeDamage ();

				if (wasKillShot)
				{
					score++;
				}
			}
		}

		RpcProcessShotEffect (result, hit.point);
	}

	[ClientRpc]
	void RpcProcessShotEffect(bool playImpact, Vector3 point)
	{
		shotEffects.PlayShotEffects ();

		if (playImpact) 
		{
			shotEffects.PlayImpactEffect (point);
		}
	}

	void OnScoreChanged(int value)
	{
		score = value;

		if (isLocalPlayer)
		{
			PlayerCanvas.canvas.SetKills (value);
		}
	}

	// ------------------ Universe stuff ------------------

	public void SetUniverseSettings(UniverseLayerSettings universe)
    {
        shootMask = universe.shootMask;
    }
}
