using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Events;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool> { }

public class Player : NetworkBehaviour
{
    [SyncVar(hook = "OnNameChanged")] public string playerName;
    [SyncVar(hook = "OnColorChanged")] public Color playerColor;

    [SerializeField] private ToggleEvent _onToggleShared;
    [SerializeField] private ToggleEvent _onToggleLocal;
    [SerializeField] private ToggleEvent _onToggleRemote;
    [SerializeField] private float _respawnTime = 5f;

    private static List<Player> _players = new List<Player>();

    private Camera _mainCamera;
    private NetworkAnimator _anim;

    private void Start()
    {
        _anim = GetComponent<NetworkAnimator>();
        // Main roaming camera of the scene
        _mainCamera = Camera.main;

        EnablePlayer();

        OnNameChanged(playerName);
        OnColorChanged(playerColor);
    }

    [ServerCallback]
    private void OnEnable()
    {
        if (!_players.Contains(this))
        {
            _players.Add(this);
        }
    }

    [ServerCallback]
    private void OnDisable()
    {
        if (_players.Contains(this))
        {
            _players.Remove(this);
        }
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
        }
        if (isLocalPlayer || playerControllerId == -1)
        {
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
        }
        if (isLocalPlayer || playerControllerId == -1)
        {
            _anim.SetTrigger("Restart");
        }

        EnablePlayer();
    }

    void OnNameChanged(string value)
    {
        playerName = value;
        gameObject.name = playerName;
        // Passing true, GetComponentInChildren search also disabled game objects, otherwise it doesn't
        GetComponentInChildren<Text>(true).text = playerName;
    }

    void OnColorChanged(Color value)
    {
        playerColor = value;
        GetComponentInChildren<RendererToggler>().ChangeColor(playerColor);
    }

    [Server]
    public void Won()
    {
        // tell other players
        for (int i = 0; i < _players.Count; i++)
        {
            _players[i].RpcGameOver(netId, name);
        }

        Invoke("BackToLobby", 5f);
    }

    [ClientRpc]
    private void RpcGameOver(NetworkInstanceId networkInstanceId, string name)
    {
        DisablePlayer();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isLocalPlayer)
        {
            if (netId == networkInstanceId)
            {
                PlayerCanvas.canvas.WriteGameStatusText("You Won!");
            }
            else
            {
                PlayerCanvas.canvas.WriteGameStatusText("Game Over!\n" + name + " Won");
            }
        }
    }

    private void BackToLobby()
    {
        FindObjectOfType<NetworkLobbyManager>().SendReturnToLobby();
    }
}
