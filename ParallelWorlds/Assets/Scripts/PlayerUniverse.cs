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

public class PlayerUniverse : NetworkBehaviour
{
    public static PlayerUniverse localPlayerUniverse;

    [SyncVar(hook = "OnPlayerUniverseStateChange")] public UniverseState universeState;

    [SerializeField] private UniverseChangeEvent _onSwitchUniverseShared;

    [Header("Swap Effect Stuff")]
    [SerializeField] private Vignette _vignette;
    [SerializeField] private Camera _camera;
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

    // --------------- NOTIFICATIONS ---------------

    private void OnLocalPlayerUniverseChanged(object sender, object args)
    {
        NotifyUniverseUpdate();
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

        NotifyUniverseUpdate();

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
    private void NotifyUniverseUpdate()
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
                    if (universeState.universe == localPlayerUniverse.universeState.universe)
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
                    if (universeState.universe == localPlayerUniverse.universeState.universe)
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
        _vignette.minRadius = _innerVignette.Evaluate(t);
        _vignette.maxRadius = _outerVignette.Evaluate(t);
        _vignette.saturation = _saturation.Evaluate(t);
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