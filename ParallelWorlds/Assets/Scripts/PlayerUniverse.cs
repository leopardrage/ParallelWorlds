using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

[System.Serializable]
public class IntEvent : UnityEvent<int> { }
public class PlayerUniverse : NetworkBehaviour
{
    [SerializeField] IntEvent onSwitchUniverseShared;

    [SyncVar(hook = "OnPlayerUniverseChange")] int playerUniverse;

    [HideInInspector] public bool Swapping { get; private set; }

    [Header("Swap Effect Stuff")]
    // [SerializeField] private Vingette _vingette;
    [SerializeField]
    private Camera _camera;
    [SerializeField] private AnimationCurve _innerVingette;
    [SerializeField] private AnimationCurve _outerVingette;
    [SerializeField] private AnimationCurve _saturation;
    [SerializeField] private AnimationCurve _fov;
    [SerializeField] private AnimationCurve _timeScale;
    [SerializeField] private AudioClip _swapAudioClip;

    private AudioSource _audio;
    private bool _swapTiggered;
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
        playerUniverse = UniverseController.Instance.GetSpawnUniverse();
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            if (!Swapping && Input.GetMouseButtonDown(1))
            {
                Swapping = true;
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
            

            if (Swapping)
            {
                if (isLocalPlayer)
                {
                    StartCoroutine(SwapAsync());
                }
                else
                {
                    // TODO: Add transition effect as seen from other players
                }
            }
            else
            {
                SwapInstant();
            }
        }
    }

    private void SwapInstant()
    {
        onSwitchUniverseShared.Invoke(playerUniverse);
    }

    /// <summary>
	/// Controls a bunch of stuff like vingette and FoV over time and change the cullmask of the player's camera after a fixed duration.
	/// </summary>
	private IEnumerator SwapAsync()
    {
        _swapTiggered = false;

        _audio.PlayOneShot(_swapAudioClip);

        for (float t = 0; t < 1.0f; t += Time.unscaledDeltaTime * 1.2f)
        {
            _camera.fieldOfView = _fov.Evaluate(t);
            /* _vingette.MinRadius = _innerVingette.Evaluate(t);
			_vingette.MaxRadius = _outerVingette.Evaluate(t);
			_vingette.Saturation = _saturation.Evaluate(t); */
            Time.timeScale = _timeScale.Evaluate(t);

            if (t > _swapTime && !_swapTiggered)
            {
                _swapTiggered = true;
               onSwitchUniverseShared.Invoke(playerUniverse);
            }

            yield return null;
        }

        // technically a huge lag spike could cause this to be missed in the coroutine so double check it here.
        if (!_swapTiggered)
        {
            _swapTiggered = true;
            onSwitchUniverseShared.Invoke(playerUniverse);
        }

        _camera.fieldOfView = _fov.Evaluate(1.0f);

        /* _vingette.MinRadius = _innerVingette.Evaluate(1.0f);
		_vingette.MaxRadius = _outerVingette.Evaluate(1.0f);
		_vingette.Saturation = 1.0f; */

        Time.timeScale = 1.0f;

        Swapping = false;
    }
}