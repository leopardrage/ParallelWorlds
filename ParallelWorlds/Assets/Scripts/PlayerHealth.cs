using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerHealth : NetworkBehaviour
{
	[SerializeField] int maxHealth = 3;
	[SyncVar (hook = "OnHealthChanged")] int health;

	Player player;


	void Awake()
	{
		player = GetComponent<Player> ();
	}

	// This method will exist only on the server
	[ServerCallback]
	void OnEnable()
	{
		health = maxHealth;
	}

	// Ensure that this method will be called only by the server
	[Server]
	public bool TakeDamage()
	{
		bool died = false;

		// if health is already 0, it means that the player has already died before, he did not JUST died, so we return false
		if (health <= 0) 
		{
			return died;
		}

		health--;
		died = health <= 0;

		RpcTakeDamage (died);

		return died;
	}

	[ClientRpc]
	void RpcTakeDamage(bool died)
	{
		if (isLocalPlayer)
		{
			PlayerCanvas.canvas.FlashDamageEffect ();
		}

		if (died) 
		{
			player.Die ();
		}
	}

	void OnHealthChanged(int value)
	{
		health = value;

		if (isLocalPlayer)
		{
			PlayerCanvas.canvas.SetHealth (value);
		}
	}
}
