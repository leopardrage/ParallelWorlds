using UnityEngine;
using UnityEngine.Networking;

public class BotSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject _botPrefab;

    [ServerCallback]
    private void Start()
    {
        GameObject obj = Instantiate(_botPrefab, transform.position, transform.rotation);
        // IMPORTANT: edit the object before call NetworkServer.Spawn()
        obj.GetComponent<NetworkIdentity>().localPlayerAuthority = false;
        obj.AddComponent<Bot>();
        NetworkServer.Spawn(obj);
    }
}
