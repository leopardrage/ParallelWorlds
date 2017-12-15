using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public interface IUniverseObserver
{
    void SetUniverseSettings(UniverseLayerSettings universe);
}

[System.Serializable]
public class UniverseChangeEvent : UnityEvent<UniverseLayerSettings> { }
[System.Serializable]
public class UniverseTransitionEvent : UnityEvent<float, float, UniverseState> { }

public class PlayerUniverse : NetworkBehaviour
{
    public static PlayerUniverse localPlayerUniverse;

    [SyncVar(hook = "OnPlayerUniverseStateChange")] public UniverseState universeState;

    [SerializeField] private UniverseChangeEvent _onSwitchUniverseShared;
    [SerializeField] private UniverseTransitionEvent _onTransitionUpdateLocal;
    [SerializeField] private UniverseTransitionEvent _onTransitionUpdateRemote;

    private readonly float _swapTime = 0.85f;
    private readonly float _transitionTime = 1.0f;

    private void Start()
    {
        // Initialize universe state for remote players (syncVar hooks are not called on variable initialization,
        // so OnPlayerUniverseChange is not called for remote players who were already in game when localPlayer
        // joined). 
        if (!isLocalPlayer)
        {
            UpdatePlayerUniverse();
        }
        else
        {
            localPlayerUniverse = this;
            this.AddObserver(OnLocalPlayerUniverseChanged, Constants.Notification.OnLocalPlayerUniverseChanged);
        }
    }

    [ServerCallback]
    private void OnEnable()
    {
        universeState = new UniverseState(
            UniverseController.Instance.GetSpawnUniverse(),
            UniverseState.TransitionState.Normal
        );
    }

    private void OnDestroy()
    {
        if (!isLocalPlayer)
        {
            this.RemoveObserver(OnLocalPlayerUniverseChanged, Constants.Notification.OnLocalPlayerUniverseChanged);
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            if (universeState.transitionState == UniverseState.TransitionState.Normal && Input.GetMouseButtonDown(1))
            {
                CmdStartSwapToOppositeUniverse();
            }
        }
    }

    // --------------- NOTIFICATION CALLBACKS ---------------

    private void OnLocalPlayerUniverseChanged(object sender, object args)
    {
        UpdateLayerSettings();
    }

    // --------------- COMMANDS ---------------

    [Command]
    private void CmdStartSwapToOppositeUniverse()
    {
        Debug.Log("CmdStartSwapToOppositeUniverse Sent");
        universeState = new UniverseState(
            universeState.universe,
            UniverseState.TransitionState.SwapOut
        );
    }

    [Command]
    private void CmdSwapToOppositeUniverse()
    {
        Debug.Log("CmdSwapToOppositeUniverse Sent");
        universeState = new UniverseState(
            UniverseController.Instance.GetOppositeUniverse(universeState.universe),
            UniverseState.TransitionState.SwapIn
        );
    }

    [Command]
    private void CmdStopSwapToOppositeUniverse()
    {
        Debug.Log("CmdStopSwapToOppositeUniverse Sent");
        universeState = new UniverseState(
            universeState.universe,
            UniverseState.TransitionState.Normal
        );

        // I need to switch universe here too, because the server must be sync in order to perform
        // raycasts correctly
        _onSwitchUniverseShared.Invoke(new UniverseLayerSettings(universeState));
    }

    // --------------- HOOKS ---------------
    // NOTE: Hooks are not called if the SyncVar is set to the same state!
    // (e.g.: currentState == (Universe_A, Normal), newState == (Universe_A, Normal) => No Hook call)

    private void OnPlayerUniverseStateChange(UniverseState universeState)
    {
        
        Debug.Log("OnPlayerUniverseStateChange Hook");
        this.universeState = universeState;

        UpdatePlayerUniverse();
    }

    // --------------- SWAP LOGIC ---------------

    private void UpdatePlayerUniverse()
    {
        if (isLocalPlayer)
        {
            this.PostNotification(Constants.Notification.OnLocalPlayerUniverseChanged);
        }

        ResetSwapEffect();

        UpdateLayerSettings();

        if (universeState.transitionState == UniverseState.TransitionState.SwapOut)
        {
            Debug.Log("Player net ID: " + netId + "swapping out of universe: " + universeState.universe);
            StartCoroutine("SwapOutAsync");
        }
        else if (universeState.transitionState == UniverseState.TransitionState.SwapIn)
        {
            Debug.Log("Player net ID: " + netId + " swapping in universe: " + universeState.universe);

            StartCoroutine("SwapInAsync");
        }
    }

    /// <summary>
	/// Updates all layers of the player's gameobjects (considering both local player and current player),
    /// set the camera cullmask and layermask for shooting raycast.
	/// </summary>
    private void UpdateLayerSettings()
    {
        if (localPlayerUniverse != null)
        {
            _onSwitchUniverseShared.Invoke(new UniverseLayerSettings(universeState, localPlayerUniverse.universeState));
        }
        else
        {
            _onSwitchUniverseShared.Invoke(new UniverseLayerSettings(universeState));
        }
    }

    // --------------- SWAP EFFECTS ---------------

    private void ResetSwapEffect()
    {
        StopCoroutine("SwapOutAsync");
        StopCoroutine("SwapInAsync");

        if (universeState.transitionState == UniverseState.TransitionState.SwapOut)
        {
            if (isLocalPlayer)
            {
                _onTransitionUpdateLocal.Invoke(_swapTime, _transitionTime, universeState);
            }
            else
            {
                _onTransitionUpdateRemote.Invoke(_swapTime, _transitionTime, universeState);
            }
        }
        else
        {
            if (isLocalPlayer)
            {
                _onTransitionUpdateLocal.Invoke(1.0f, _transitionTime, universeState);
            }
            else
            {
                _onTransitionUpdateRemote.Invoke(1.0f, _transitionTime, universeState);
            }
        }
    }
    
	private IEnumerator SwapOutAsync()
    {
        for (float t = 0; t < _swapTime; t += Time.unscaledDeltaTime * 1.2f)
        {
            if (isLocalPlayer)
            {
                _onTransitionUpdateLocal.Invoke(t, _transitionTime, universeState);
            }
            else
            {
                _onTransitionUpdateRemote.Invoke(t, _transitionTime, universeState);
            }

            yield return null;
        }

        if (isLocalPlayer)
        {
            CmdSwapToOppositeUniverse();
        }
    }

    private IEnumerator SwapInAsync()
    {
        for (float t = _swapTime; t < _transitionTime; t += Time.unscaledDeltaTime * 1.2f)
        {
            if (isLocalPlayer)
            {
                _onTransitionUpdateLocal.Invoke(t, _transitionTime, universeState);
            }
            else
            {
                _onTransitionUpdateRemote.Invoke(t, _transitionTime, universeState);
            }

            yield return null;
        }

        if (isLocalPlayer)
        {
            CmdStopSwapToOppositeUniverse();
        }
    }
}