using UnityEngine;
using Prototype.NetworkLobby;
using UnityEngine.Networking;

public class CustomLobbyHook : LobbyHook
{
    public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer)
    {
        LobbyPlayer lPlayer = lobbyPlayer.GetComponent<LobbyPlayer>();
        Player gPlayer = gamePlayer.GetComponent<Player>();

        gPlayer.playerName = lPlayer.playerName;
        gPlayer.playerColor = lPlayer.playerColor;
    }
}