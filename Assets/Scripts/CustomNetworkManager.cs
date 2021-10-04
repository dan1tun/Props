using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

// NetworkManager personalizado, para poder controlar mejor los spawns y otras lógicas
//

[AddComponentMenu("")]
public class CustomNetworkManager : NetworkManager
{
    [Header("Room configuration")]
    [SerializeField] private int maxPlayers, maxHunters;

    [Header("Player prefabs")]
    [SerializeField] private GameObject hunterPrefab, propPrefab;

    [Header("Scene configuration")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();


    /// <summary>Called on server when a client requests to add the player. Adds playerPrefab by default. Can be overwritten.</summary>
    // The default implementation for this function creates a new player object from the playerPrefab.
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Transform startPos = GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);
        player.GetComponent<PlayerController>().menuScript = this.GetComponent<MenuScript>();

        // instantiating a "Player" prefab gives it the name "Player(clone)"
        // => appending the connectionId is WAY more useful for debugging!
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}
