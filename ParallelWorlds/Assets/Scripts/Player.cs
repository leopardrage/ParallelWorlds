using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool> { }

public class Player : NetworkBehaviour
{
    [SerializeField] private ToggleEvent _onToggleShared;
    [SerializeField] private ToggleEvent _onToggleLocal;
    [SerializeField] private ToggleEvent _onToggleRemote;
    [SerializeField] private float _respawnTime = 5f;

    private Camera _mainCamera;
    private NetworkAnimator _anim;

    private void Start()
    {
        _anim = GetComponent<NetworkAnimator>();
        // Main roaming camera of the scene
        _mainCamera = Camera.main;

        EnablePlayer();
    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        _anim.animator.SetFloat("Speed", Input.GetAxis("Vertical"));
        _anim.animator.SetFloat("Strafe", Input.GetAxis("Horizontal"));
    }

    private void DisablePlayer()
    {
        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.HideReticule();

            // Activate roaming camera
            _mainCamera.gameObject.SetActive(true);
        }

        _onToggleShared.Invoke(false);

        if (isLocalPlayer)
        {
            _onToggleLocal.Invoke(false);
        }
        else
        {
            _onToggleRemote.Invoke(false);
        }
    }

    private void EnablePlayer()
    {
        Debug.Log("Enable Player net id: " + netId);
        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.Initialize();

            // Deactivate roaming camera, switching to player camera
            _mainCamera.gameObject.SetActive(false);
        }

        _onToggleShared.Invoke(true);

        if (isLocalPlayer)
        {
            _onToggleLocal.Invoke(true);
        }
        else
        {
            _onToggleRemote.Invoke(true);
        }
    }

    public void Die()
    {
        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.WriteGameStatusText("You Died!");
            PlayerCanvas.canvas.PlayDeathAudio();

            _anim.SetTrigger("Died");
        }

        DisablePlayer();

        Invoke("Respawn", _respawnTime);
    }

    private void Respawn()
    {
        if (isLocalPlayer)
        {
            Transform spawn = NetworkManager.singleton.GetStartPosition();
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;

            _anim.SetTrigger("Restart");
        }

        EnablePlayer();
    }
}
