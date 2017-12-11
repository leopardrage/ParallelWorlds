using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool> { }
[System.Serializable]
public class IntEvent : UnityEvent<int> { }

public class Player : NetworkBehaviour
{
    [SerializeField] ToggleEvent onToggleShared;
    [SerializeField] ToggleEvent onToggleLocal;
    [SerializeField] ToggleEvent onToggleRemote;
    [SerializeField] float respawnTime = 5f;
    [SerializeField] IntEvent onSwitchUniverseShared;


    Camera mainCamera;
    NetworkAnimator anim;
    [SyncVar(hook = "OnPlayerUniverseChange")] int playerUniverse;

    void Start()
    {
        anim = GetComponent<NetworkAnimator>();
        // Main roaming camera of the scene
        mainCamera = Camera.main;

        EnablePlayer();

		// Initialize universe state for remote players (syncVar hooks are not called on variable initialization,
		// so OnPlayerUniverseChange is not called for remote players who were already in game when localPlayer
		// joined). 
        if (!isLocalPlayer)
        {
            UpdatePlayerUniverse();
        }
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
            PlayerCanvas.canvas.HideReticule();

            // Activate roaming camera
            mainCamera.gameObject.SetActive(true);
        }

        onToggleShared.Invoke(false);

        if (isLocalPlayer)
        {
            onToggleLocal.Invoke(false);
        }
        else
        {
            onToggleRemote.Invoke(false);
        }
    }

    void EnablePlayer()
    {
        Debug.Log("Enable Player net id: " + netId);
        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.Initialize();

            // Deactivate roaming camera, switching to player camera
            mainCamera.gameObject.SetActive(false);

            CmdGetNewUniverse();
        }

        onToggleShared.Invoke(true);

        if (isLocalPlayer)
        {
            onToggleLocal.Invoke(true);
        }
        else
        {
            onToggleRemote.Invoke(true);
        }
    }

    public void Die()
    {
        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.WriteGameStatusText("You Died!");
            PlayerCanvas.canvas.PlayDeathAudio();

            anim.SetTrigger("Died");
        }

        DisablePlayer();

        Invoke("Respawn", respawnTime);
    }

    void Respawn()
    {
        if (isLocalPlayer)
        {
            Transform spawn = NetworkManager.singleton.GetStartPosition();
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;

            anim.SetTrigger("Restart");
        }

        EnablePlayer();
    }

    [Command]
    void CmdGetNewUniverse()
    {
        playerUniverse = UniverseController.Instance.GetSpawnUniverse();
    }

    void OnPlayerUniverseChange(int universe)
    {
        playerUniverse = universe;

        UpdatePlayerUniverse();
    }

    void UpdatePlayerUniverse()
    {
        if (playerUniverse != 0)
        {
            Debug.Log("Set Universe: " + playerUniverse + " for player net ID: " + netId);
            onSwitchUniverseShared.Invoke(playerUniverse);
        }
    }
}
