using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

[System.Serializable]
public class IntEvent : UnityEvent<int> { }
public class PlayerUniverse : NetworkBehaviour
{
    [System.Serializable]
    public struct PlayerUniverseState
    {
        public enum TransitionState
        {
            Normal,
            SwapIn,
            SwapOut
        }

        public PlayerUniverseState(int universe, TransitionState transitionState)
        {
            this.universe = universe;
            this.transitionState = transitionState;
        }

        public int universe;
        public TransitionState transitionState;
    }

    [SerializeField] IntEvent onSwitchUniverseShared;

    [SyncVar(hook = "OnPlayerUniverseStateChange")] PlayerUniverseState playerUniverseState;

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
    [SerializeField] private RendererToggler _body;

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
    }

    [ServerCallback]
    private void OnEnable()
    {
        playerUniverseState = new PlayerUniverseState(
            UniverseController.Instance.GetSpawnUniverse(),
            PlayerUniverseState.TransitionState.Normal
        );
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
        playerUniverseState = new PlayerUniverseState(
            playerUniverseState.universe,
            PlayerUniverseState.TransitionState.SwapOut
        );
    }

    [Command]
    void CmdSwapToOppositeUniverse()
    {
        playerUniverseState = new PlayerUniverseState(
            UniverseController.Instance.GetOppositeUniverse(playerUniverseState.universe),
            PlayerUniverseState.TransitionState.SwapIn
        );
    }

    [Command]
    void CmdStopSwapToOppositeUniverse()
    {
        playerUniverseState = new PlayerUniverseState(
            playerUniverseState.universe,
            PlayerUniverseState.TransitionState.Normal
        );
    }

    // --------------- HOOKS ---------------

    void OnPlayerUniverseStateChange(PlayerUniverseState universeState)
    {
        playerUniverseState = universeState;

        UpdatePlayerUniverse();
    }

    // --------------- SWAP LOGIC ---------------

    void UpdatePlayerUniverse()
    {
        ResetSwapEffect();

        if (playerUniverseState.universe != 0)
        {
            if (playerUniverseState.transitionState == PlayerUniverseState.TransitionState.Normal)
            {
                SwapInstant();
            }
            else
            {
                if (playerUniverseState.transitionState == PlayerUniverseState.TransitionState.SwapOut)
                {
                    Debug.Log("Player net ID: " + netId + "swapping out of universe: " + playerUniverseState.universe);
                    StartCoroutine("SwapOutAsync");
                }
                else
                {
                    Debug.Log("Player net ID: " + netId + " swapping in universe: " + playerUniverseState.universe);
                    onSwitchUniverseShared.Invoke(playerUniverseState.universe);
                    StartCoroutine("SwapInAsync");
                }
            }
        }
    }

    // --------------- SWAP EFFECTS ---------------

    private void SwapInstant()
    {
        Debug.Log("Player net ID: " + netId + " arrived in universe: " + playerUniverseState.universe);
        onSwitchUniverseShared.Invoke(playerUniverseState.universe);
    }

    private void ResetSwapEffect()
    {
        StopCoroutine("SwapOutAsync");
        StopCoroutine("SwapInAsync");

        if (playerUniverseState.transitionState == PlayerUniverseState.TransitionState.SwapOut)
        {
            ApplyEffectLocal(_swapTime);
            ApplyEffectRemote(_swapTime);
        }
        else
        {
            ApplyEffectLocal(1.0f);
            ApplyEffectRemote(1.0f);
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
                ApplyEffectRemote(t);
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
                ApplyEffectRemote(t);
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

    private void ApplyEffectRemote(float t)
    {
        // TODO: replace with cooler effect
        if (_body != null)
        {
            if (t < _swapTime)
            {
                _body.ChangeColor(Color.magenta);
            }
            else if (t < 1.0f)
            {
                _body.ChangeColor(Color.green);
            }
            else
            {
                _body.ChangeColor(Color.white);
            }
        }
    }
}