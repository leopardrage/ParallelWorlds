using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int _maxHealth = 3;
    [SyncVar(hook = "OnHealthChanged")] private int _health;

    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    // This method will exist only on the server
    [ServerCallback]
    private void OnEnable()
    {
        _health = _maxHealth;
    }

    // Ensure that this method will be called only by the server
    [Server]
    public bool TakeDamage()
    {
        bool died = false;

        // if health is already 0, it means that the player has already died before, he did not JUST died, so we return false
        if (_health <= 0)
        {
            return died;
        }

        _health--;
        died = _health <= 0;

        RpcTakeDamage(died);

        return died;
    }

    [ClientRpc]
    private void RpcTakeDamage(bool died)
    {
        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.FlashDamageEffect();
        }

        if (died)
        {
            _player.Die();
        }
    }

    private void OnHealthChanged(int value)
    {
        _health = value;

        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.SetHealth(value);
        }
    }
}
