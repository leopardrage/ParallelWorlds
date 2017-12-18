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

/// <summary>
/// This component handles the core logic of universe swapping.
/// <remarks>
/// <para>
/// Universe swap happens in three steps
/// </para>
/// <para>
/// 1) When the player sends the swap input, the universe state is changed to SwapOut from the current universe
/// and a coroutine starts to update any effect related to the swapOut every frame;
/// </para>
/// <para>
/// 2) When the coroutine reaches the swap time, the player effectively switch to the new universe, changing his
/// layers, camera cullmask and raycast layers for shooting;
/// </para>
/// <para>
/// 3) Right after that the universe state switch to SwapIn in the new universe and a second coroutine starts
/// to update any effect related to the swapOut every frame.
/// </para>
/// <para>
/// At the end, the status return to Normal in the new universe.
/// </para>
/// <para>
/// IMPORTANT NOTE: to display lights correctly on all clients for a given remote player, his layers are not
/// the same on every client, but are based on the current universe of the local player, so the user can see the
/// the remote player shaded correctly by the lights of his universe.
/// </para>
/// <para>
/// Example: let's assume player 1 is swapping Out from A to B (so he is still logically in universe A),
/// player 2 is in universe A and player 3 in universe B. Player 1 will have layer UniverseA in the client where player 2 is local player,
/// but will have layer UniverseBcollisionA (visible on B but with collisions still on A) in the client where player 3 is local player.
/// </para>
/// </remarks>
/// </summary>
public class PlayerUniverse : NetworkBehaviour
{
    // Helper variable to allow other classes to check the local player universe state
    public static PlayerUniverse localPlayerUniverse;

    [SyncVar(hook = "OnPlayerUniverseStateChange")] public UniverseState universeState = new UniverseState(Universe.UniverseUndefined, UniverseState.TransitionState.Normal);

    [SerializeField] private UniverseChangeEvent _onSwitchUniverseShared;
    [SerializeField] private UniverseTransitionEvent _onTransitionUpdateLocal;
    [SerializeField] private UniverseTransitionEvent _onTransitionUpdateRemote;

    private readonly float _swapTime = 0.85f;
    private readonly float _transitionTime = 1.0f;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            // Register this remote for local player universe changes, so he can update his layers accordingly
            this.AddObserver(OnLocalPlayerUniverseChanged, Constants.Notification.OnLocalPlayerUniverseChanged);
        }
        else
        {
            localPlayerUniverse = this;
        }
        // Initialize universe state for all player (syncVar hooks are not called on variable initialization)
        UpdatePlayerUniverse();
    }

    [ServerCallback]
    private void OnEnable()
    {
        // Setup initial state. NOTE:
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
            ((universeState.universe == Universe.UniverseB) ? Universe.UniverseA : Universe.UniverseB),
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
            // This happens if localPlayer is still not set or if it's server. Either way, it's fine to just set
            // layers to UniverseA or UniverseB, based on the current universe.
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
        for (float t = 0; t < _swapTime; t += Time.deltaTime * 1.2f)
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

        if (isLocalPlayer || playerControllerId == -1)
        {
            CmdSwapToOppositeUniverse();
        }
    }

    private IEnumerator SwapInAsync()
    {
        for (float t = _swapTime; t < _transitionTime; t += Time.deltaTime * 1.2f)
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

        if (isLocalPlayer || playerControllerId == -1)
        {
            CmdStopSwapToOppositeUniverse();
        }
    }

    // --------------- BOT STUFF ---------------

    public void SwapUniverseForBot()
    {
        if (universeState.transitionState == UniverseState.TransitionState.Normal)
        {
            CmdStartSwapToOppositeUniverse();
        }
    }
}