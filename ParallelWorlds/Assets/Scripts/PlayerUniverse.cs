using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

[System.Serializable]
public struct PlayerUniverseState
{
    public enum TransitionState
    {
        Normal,
        SwapIn,
        SwapOut
    }

    public PlayerUniverseState(Universe universe, TransitionState transitionState)
    {
        this.universe = universe;
        this.transitionState = transitionState;
    }

    public Universe universe;
    public TransitionState transitionState;
}

[System.Serializable]
public struct UniverseLayerSettings
{
    public int layer;
    public LayerMask cullingMask;
    public LayerMask shootMask;

    public UniverseLayerSettings(PlayerUniverseState currentUniverseState) : this()
    {
        this.layer = GetLayer(currentUniverseState);
        this.cullingMask = GetCullMask(currentUniverseState);
        this.shootMask = GetShootMask(currentUniverseState);
    }
    public UniverseLayerSettings(PlayerUniverseState currentUniverseState, PlayerUniverseState localUniverseState) : this()
    {
        this.layer = GetLayer(currentUniverseState, localUniverseState);
        this.cullingMask = GetCullMask(currentUniverseState);
        this.shootMask = GetShootMask(currentUniverseState);
    }

    private int GetLayer(PlayerUniverseState currentUniverseState)
    {
        // Both visual and physics set to the current universe
        return (currentUniverseState.universe == Universe.UniverseA) ? 8 : 9;
    }
    private int GetLayer(PlayerUniverseState currentUniverseState, PlayerUniverseState localUniverseState)
    {
        // Normal State: 
        if (currentUniverseState.transitionState == PlayerUniverseState.TransitionState.Normal)
        {
            // Both visual and physics set to the current universe
            return (currentUniverseState.universe == Universe.UniverseA) ? 8 : 9;
        }
        // Swapping
        else
        {
            // Same universe as the Local Player:
            if (localUniverseState.universe == currentUniverseState.universe)
            {
                // Both visual and physics set to the current universe
                return (currentUniverseState.universe == Universe.UniverseA) ? 8 : 9;
            }
            // Different universe from the Local Player:
            else
            {
                // Physics set to the current universe but visual set to match the local player's
                return (localUniverseState.universe == Universe.UniverseA) ? 10 : 11;
            }
        }
    }

    private LayerMask GetCullMask(PlayerUniverseState currentUniverseState)
    {
        if (currentUniverseState.universe == Universe.UniverseA)
        {
            return LayerMask.GetMask("Universe_A", "Universe_A_Collision_B");
        }
        else
        {
            return LayerMask.GetMask("Universe_B", "Universe_B_Collision_A");
        }
    }

    private LayerMask GetShootMask(PlayerUniverseState currentUniverseState)
    {
        if (currentUniverseState.universe == Universe.UniverseA)
        {
            return LayerMask.GetMask("Universe_A", "Universe_B_Collision_A");
        }
        else
        {
            return LayerMask.GetMask("Universe_B", "Universe_A_Collision_B");
        }
    }
}

[System.Serializable]
public class UniverseChangeEvent : UnityEvent<UniverseLayerSettings> { }
public class PlayerUniverse : NetworkBehaviour
{
    public static PlayerUniverse localPlayerUniverse;
    [SerializeField] UniverseChangeEvent onSwitchUniverseShared;

    [SyncVar(hook = "OnPlayerUniverseStateChange")] public PlayerUniverseState playerUniverseState;

    [Header("Swap Effect Stuff")]
    [SerializeField]
    private Vignette _vignette;
    [SerializeField]
    private Camera _camera;
    [SerializeField] private AnimationCurve _innerVignette;
    [SerializeField] private AnimationCurve _outerVignette;
    [SerializeField] private AnimationCurve _saturation;
    [SerializeField] private AnimationCurve _fov;
    [SerializeField] private AudioClip _swapAudioClip;
    // TODO: remove (Just to test remote swap effect logic correctness)
    [SerializeField] private SwapEffectRemote _body;
    [SerializeField] private SwapEffectRemote _gun;

    private AudioSource _audio;
    private readonly float _swapTime = 0.85f;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
    }

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
            this.AddObserver(OnLocalPlayerUniverseChanged, "OnLocalPlayerUniverseChanged");
        }
    }

    void OnLocalPlayerUniverseChanged(object sender, object args)
    {
        if (localPlayerUniverse != null)
        {
            onSwitchUniverseShared.Invoke(new UniverseLayerSettings(playerUniverseState, localPlayerUniverse.playerUniverseState));
        }
        else
        {
            onSwitchUniverseShared.Invoke(new UniverseLayerSettings(playerUniverseState));
        }
    }

    [ServerCallback]
    private void OnEnable()
    {
        playerUniverseState = new PlayerUniverseState(
            UniverseController.Instance.GetSpawnUniverse(),
            PlayerUniverseState.TransitionState.Normal
        );
    }

    private void OnDestroy()
    {
        if (!isLocalPlayer)
        {
            this.RemoveObserver(OnLocalPlayerUniverseChanged, "OnLocalPlayerUniverseChanged");
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            if (playerUniverseState.transitionState == PlayerUniverseState.TransitionState.Normal && Input.GetMouseButtonDown(1))
            {
                CmdStartSwapToOppositeUniverse();
            }
        }
    }

    // --------------- COMMANDS ---------------

    [Command]
    void CmdStartSwapToOppositeUniverse()
    {
        Debug.Log("CmdStartSwapToOppositeUniverse Sent");
        playerUniverseState = new PlayerUniverseState(
            playerUniverseState.universe,
            PlayerUniverseState.TransitionState.SwapOut
        );
    }

    [Command]
    void CmdSwapToOppositeUniverse()
    {
        Debug.Log("CmdSwapToOppositeUniverse Sent");
        playerUniverseState = new PlayerUniverseState(
            UniverseController.Instance.GetOppositeUniverse(playerUniverseState.universe),
            PlayerUniverseState.TransitionState.SwapIn
        );
    }

    [Command]
    void CmdStopSwapToOppositeUniverse()
    {
        Debug.Log("CmdStopSwapToOppositeUniverse Sent");
        playerUniverseState = new PlayerUniverseState(
            playerUniverseState.universe,
            PlayerUniverseState.TransitionState.Normal
        );

        // I need to switch universe here too, because the server must be sync in order to perform
        // raycasts correctly
        onSwitchUniverseShared.Invoke(new UniverseLayerSettings(playerUniverseState));
    }

    // --------------- HOOKS ---------------
    // NOTE: Hooks are not called if the SyncVar is set to the same state!
    // (e.g.: currentState == (Universe_A, Normal), newState == (Universe_A, Normal) => No Hook call)

    void OnPlayerUniverseStateChange(PlayerUniverseState universeState)
    {
        
        Debug.Log("OnPlayerUniverseStateChange Hook");
        playerUniverseState = universeState;

        UpdatePlayerUniverse();
    }

    // --------------- SWAP LOGIC ---------------

    void UpdatePlayerUniverse()
    {
        if (isLocalPlayer)
        {
            this.PostNotification("OnLocalPlayerUniverseChanged");
        }

        ResetSwapEffect();

        if (localPlayerUniverse != null)
        {
            onSwitchUniverseShared.Invoke(new UniverseLayerSettings(playerUniverseState, localPlayerUniverse.playerUniverseState));
        }
        else
        {
            onSwitchUniverseShared.Invoke(new UniverseLayerSettings(playerUniverseState));
        }

        if (playerUniverseState.transitionState == PlayerUniverseState.TransitionState.SwapOut)
        {
            Debug.Log("Player net ID: " + netId + "swapping out of universe: " + playerUniverseState.universe);
            StartCoroutine("SwapOutAsync");
        }
        else if (playerUniverseState.transitionState == PlayerUniverseState.TransitionState.SwapIn)
        {
            Debug.Log("Player net ID: " + netId + " swapping in universe: " + playerUniverseState.universe);

            StartCoroutine("SwapInAsync");
        }
    }

    // --------------- SWAP EFFECTS ---------------

    private void ResetSwapEffect()
    {
        StopCoroutine("SwapOutAsync");
        StopCoroutine("SwapInAsync");

        if (playerUniverseState.transitionState == PlayerUniverseState.TransitionState.SwapOut)
        {
            ApplyEffectLocal(_swapTime);
            ApplyEffectRemote(_swapTime, false);
        }
        else
        {
            ApplyEffectLocal(1.0f);
            ApplyEffectRemote(1.0f, false);
        }
    }

    /// <summary>
	/// Controls a bunch of stuff like vingette and FoV over time and change the cullmask of the player's camera after a fixed duration.
	/// </summary>
	private IEnumerator SwapOutAsync()
    {
        _audio.PlayOneShot(_swapAudioClip);

        for (float t = 0; t < _swapTime; t += Time.unscaledDeltaTime * 1.2f)
        {
            if (isLocalPlayer)
            {
                ApplyEffectLocal(t);
            }
            else
            {
                if (localPlayerUniverse != null)
                {
                    if (playerUniverseState.universe == localPlayerUniverse.playerUniverseState.universe)
                    {
                        ApplyEffectRemote(t, true);
                    }
                    else
                    {
                        ApplyEffectRemote(t, false);
                    }
                }
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
        for (float t = _swapTime; t < 1.0f; t += Time.unscaledDeltaTime * 1.2f)
        {
            if (isLocalPlayer)
            {
                ApplyEffectLocal(t);
            }
            else
            {
                if (localPlayerUniverse != null)
                {
                    if (playerUniverseState.universe == localPlayerUniverse.playerUniverseState.universe)
                    {
                        ApplyEffectRemote(t, false);
                    }
                    else
                    {
                        ApplyEffectRemote(t, true);
                    }
                }
            }

            yield return null;
        }

        if (isLocalPlayer)
        {
            CmdStopSwapToOppositeUniverse();
        }
    }

    private void ApplyEffectLocal(float t)
    {
        _camera.fieldOfView = _fov.Evaluate(t);
        _vignette.MinRadius = _innerVignette.Evaluate(t);
        _vignette.MaxRadius = _outerVignette.Evaluate(t);
        _vignette.Saturation = _saturation.Evaluate(t);
    }

    private void ApplyEffectRemote(float t, bool reverse)
    {
        if (_body != null)
        {
            _body.ApplyEffect(t, reverse);
        }
        if (_gun != null)
        {
            _gun.ApplyEffect(t, reverse);
        }
    }
}