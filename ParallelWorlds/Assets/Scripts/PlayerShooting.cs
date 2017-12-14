using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerShooting : NetworkBehaviour, IUniverseObserver
{
    [SerializeField] private float _shotCooldown = .3f;
    [SerializeField] private Transform _firePosition;
    [SerializeField] private ShotEffectsManager _shotEffects;

    [SyncVar(hook = "OnScoreChanged")] private int _score;

    private float _ellapsedTime;
    private bool _canShoot;
    private LayerMask _shootMask;

    void Start()
    {
        _shotEffects.Initialize();

        if (isLocalPlayer)
        {
            _canShoot = true;
        }
    }

    [ServerCallback]
    void OnEnable()
    {
        _score = 0;
    }

    void Update()
    {
        if (!_canShoot)
        {
            return;
        }

        _ellapsedTime += Time.deltaTime;

        if (Input.GetButtonDown("Fire1") && _ellapsedTime > _shotCooldown)
        {
            _ellapsedTime = 0;

            // Note, we pass these parameters because they are localPlayer's. Inside CmdFireShot method we are on the server and those data
            // will be of the version of the player on the server, that is not the one we are interested in.
            CmdFireShot(_firePosition.position, _firePosition.forward);
        }
    }

    [Command]
    void CmdFireShot(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;

        Ray ray = new Ray(origin, direction);
        Debug.DrawRay(ray.origin, ray.direction * 3f, Color.red, 1f);

        bool result = Physics.Raycast(ray, out hit, 50f, _shootMask);

        if (result)
        {
            PlayerHealth enemy = hit.transform.GetComponent<PlayerHealth>();
            if (enemy != null)
            {
                bool wasKillShot = enemy.TakeDamage();

                if (wasKillShot)
                {
                    _score++;
                }
            }
        }

        RpcProcessShotEffect(result, hit.point);
    }

    [ClientRpc]
    void RpcProcessShotEffect(bool playImpact, Vector3 point)
    {
        _shotEffects.PlayShotEffects();

        if (playImpact)
        {
            _shotEffects.PlayImpactEffect(point);
        }
    }

    void OnScoreChanged(int value)
    {
        _score = value;

        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.SetKills(value);
        }
    }

    // ------------- IUniverseObserver ---------------

    public void SetUniverseSettings(UniverseLayerSettings universe)
    {
        _shootMask = universe.shootMask;
    }
}
