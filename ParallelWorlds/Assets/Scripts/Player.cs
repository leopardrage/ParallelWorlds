using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool>{}

public class Player : NetworkBehaviour 
{
	[SerializeField] ToggleEvent onToggleShared;
	[SerializeField] ToggleEvent onToggleLocal;
	[SerializeField] ToggleEvent onToggleRemote;
	[SerializeField] float respawnTime = 5f;

	GameObject mainCamera;
	NetworkAnimator anim;

	void Start()
	{
		anim = GetComponent<NetworkAnimator>();
		// Main roaming camera of the scene
		mainCamera = Camera.main.gameObject;

		EnablePlayer ();
	}

	private void Update() 
	{
		if (!isLocalPlayer)
		{
			return;
		}

		anim.animator.SetFloat("Speed", Input.GetAxis("Vertical"));
		anim.animator.SetFloat("Strafe", Input.GetAxis("Horizontal"));
	}

	void DisablePlayer()
	{
		if (isLocalPlayer) 
		{
			PlayerCanvas.canvas.HideReticule ();

			// Activate roaming camera
			mainCamera.SetActive (true);
		}

		onToggleShared.Invoke (false);

		if (isLocalPlayer)
		{
			onToggleLocal.Invoke (false);
		}
		else
		{
			onToggleRemote.Invoke (false);
		}
	}

	void EnablePlayer ()
	{
		if (isLocalPlayer)
		{
			PlayerCanvas.canvas.Initialize ();

			// Deactivate roaming camera, switching to player camera
			mainCamera.SetActive (false);
		}

		onToggleShared.Invoke (true);

		if (isLocalPlayer)
		{
			onToggleLocal.Invoke (true);
		}
		else
		{
			onToggleRemote.Invoke (true);
		}
	}

	public void Die()
	{
		if (isLocalPlayer)
		{
			PlayerCanvas.canvas.WriteGameStatusText ("You Died!");
			PlayerCanvas.canvas.PlayDeathAudio ();
			
			anim.SetTrigger("Died");
		}
		
		DisablePlayer ();

		Invoke ("Respawn", respawnTime);
	}

	void Respawn()
	{
		if (isLocalPlayer) 
		{
			Transform spawn = NetworkManager.singleton.GetStartPosition ();
			transform.position = spawn.position;
			transform.rotation = spawn.rotation;

			anim.SetTrigger("Restart");
		}

		EnablePlayer ();
	}
}
