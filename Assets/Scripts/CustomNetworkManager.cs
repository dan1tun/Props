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

    int currentHunters = 0, currentProps = 0, currentPlayers = 0;
    Dictionary<string, Enums.PlayerType> listOfConnections = new Dictionary<string, Enums.PlayerType>();

    public override void Start()
    {
        base.Start();

        //registramos los prefabs de los jugadores
        if (hunterPrefab != null)
            NetworkClient.RegisterPrefab(hunterPrefab);
        if (propPrefab != null)
            NetworkClient.RegisterPrefab(propPrefab);
    }

    public override void OnStartServer() {
        base.OnStartServer();
    }

    /// <summary>Called on server when a client requests to add the player. Adds playerPrefab by default. Can be overwritten.</summary>
    // The default implementation for this function creates a new player object from the playerPrefab.
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("OnServerAddPlayer");

        // should it be a hunter or a prop?
        bool spawnHunter = false;
        if (currentHunters < maxHunters)
        {
            if (currentHunters == 0 && Mathf.Round(Random.Range(0,1)) == 1) //first player will have a 1/2 prob. to be a hunter (TODO: do it in a pre-lobby!)
                spawnHunter = true;
            else if (currentHunters >= currentProps) //if theres no props, then it is a prop
                spawnHunter = false;
            else
            {
                int baseProbability = (currentProps - currentHunters - 1) / 2;
                // la proporción 3 a 1 me parece aceptable. La 4 a 1 es demasiado. La partida ideal será de 4 vs 2. Simulación:
                // PROPS    HUNTERS =>  PROPS   HUNTERS
                // 0        0           0       1
                // 0        1           1       1
                // 1        1           2       1
                // 2        1           3       1       => A partir de aquí, puede ir a 4 / 1 y luego 4 / 2, o 3 / 2 y luego 4 / 2


            }
        }
        GameObject player;
        Transform startPos = GetStartPosition();
        if (spawnHunter)
        {
            currentHunters++;
            player = startPos != null
                ? Instantiate(hunterPrefab, startPos.position, startPos.rotation)
                : Instantiate(hunterPrefab);
            listOfConnections.Add(conn.address, Enums.PlayerType.Hunter);
        }
        else
        {
            currentProps++;
            player = startPos != null
                ? Instantiate(propPrefab, startPos.position, startPos.rotation)
                : Instantiate(propPrefab);
            listOfConnections.Add(conn.address, Enums.PlayerType.Prop);
        }
        currentPlayers++;


        player.GetComponent<PlayerController>().menuScript = this.GetComponent<MenuScript>();

        // instantiating a "Player" prefab gives it the name "Player(clone)"
        // => appending the connectionId is WAY more useful for debugging!
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        //check what type of player it was
        switch (listOfConnections[conn.address])
        {
            case Enums.PlayerType.Hunter:
                currentHunters--;
                break;
            case Enums.PlayerType.Prop:
                currentPlayers--;
                break;
        }
        currentPlayers--;

        base.OnClientDisconnect(conn);
    }
}