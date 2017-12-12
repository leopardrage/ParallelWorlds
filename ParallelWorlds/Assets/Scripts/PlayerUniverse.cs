using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

[System.Serializable]
public class IntEvent : UnityEvent<int> { }
public class PlayerUniverse : NetworkBehaviour
{
    [SerializeField] IntEvent onSwitchUniverseShared;

    [SyncVar(hook = "OnPlayerUniverseChange")] int playerUniverse;

    private void Start()
    {
        // Initialize universe state for remote players (syncVar hooks are not called on variable initialization,
        // so OnPlayerUniverseChange is not called for remote players who were already in game when localPlayer
        // joined). 
        if (!isLocalPlayer)
        {
            UpdatePlayerUniverse();
        }
    }

    [ServerCallback]
    private void OnEnable()
    {
        playerUniverse = UniverseController.Instance.GetSpawnUniverse();
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            if (Input.GetMouseButtonDown(1))
            {
                CmdSwitchToOppositeUniverse();
            }
        }
    }

    [Command]
    void CmdSwitchToOppositeUniverse()
    {
        playerUniverse = UniverseController.Instance.GetOppositeUniverse(playerUniverse);
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