using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class Bot : NetworkBehaviour
{
    public bool botCanShoot = true;

    [SerializeField] private float _shotCooldown = 1f;

    private PlayerShooting _playerShooting;
    private PlayerUniverse _playerUniverse;
    private NetworkAnimator _anim;
    private float _ellapsedTime;

    private void Awake()
    {
        _playerShooting = GetComponent<PlayerShooting>();
        _playerUniverse = GetComponent<PlayerUniverse>();
        _anim = GetComponent<NetworkAnimator>();

        GetComponent<Player>().playerName = "Bot";
        GetComponent<Player>().playerColor = Color.white;
    }

    [ServerCallback]
    private void Update()
    {
        _anim.animator.SetFloat("Speed", 0f);
        _anim.animator.SetFloat("Strafe", 0f);

        if (Input.GetKey(KeyCode.Keypad8))
        {
            _anim.animator.SetFloat("Speed", 1f);
        }

        if (Input.GetKey(KeyCode.Keypad2))
        {
            _anim.animator.SetFloat("Speed", -1f);
        }

        if (Input.GetKey(KeyCode.Keypad4))
        {
            _anim.animator.SetFloat("Strafe", -1f);
        }

        if (Input.GetKey(KeyCode.Keypad6))
        {
            _anim.animator.SetFloat("Strafe", 1f);
        }

        if (Input.GetKeyDown(KeyCode.Keypad7))
        {
            _anim.SetTrigger("Died");
        }

        if (Input.GetKeyDown(KeyCode.Keypad9))
        {
            _anim.SetTrigger("Restart");
        }

        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            _playerUniverse.SwapUniverseForBot();
        }

        BotAutoFire();
    }

    [Server]
    private void BotAutoFire()
    {
        _ellapsedTime += Time.deltaTime;

        if (_ellapsedTime < _shotCooldown)
        {
            return;
        }

        _ellapsedTime = 0f;
        if (_playerShooting.enabled)
        {
            _playerShooting.FireAsBot();
        }
    }
}
