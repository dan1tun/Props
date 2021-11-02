using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

// NetworkManager personalizado, para poder controlar mejor los spawns y otras lógicas
//

[AddComponentMenu("")]
public class CustomNetworkManager : NetworkManager
{
    [Header("Room public configuration")] // para futuro: los jugadores podran modificarlo
    [SerializeField] private int maxPlayers;
    [SerializeField] private int maxHunters;
    public float initialTime;
    public float preRoundTime;
    public float roundTime;
    public float escapeTime;

    [Header("Scene configuration")] // necesario para este escenario
    public GameObject propDoor;
    public GameObject hunterDoor;
    [SerializeField] private Transform hunterSpawn;
    [SerializeField] private Transform propSpawn;


    [Header("Player prefabs")]
    [SerializeField] private GameObject hunterPrefab;
    [SerializeField] private GameObject propPrefab;



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

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerChangeScene(sceneName);

        Configurar();
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        base.OnClientSceneChanged(conn);

        Configurar();
    }

    private void Configurar()
    {
        // Buscamos la configuración de la escena (debe estar en el script "scene config" dentro del objeto "--- CONFIGURATION ---")
        GameObject sceneConfigObject = GameObject.Find("--- CONFIGURATION ---");
        if (sceneConfigObject)
        {
            SceneConfig config = sceneConfigObject.GetComponent<SceneConfig>();
            propDoor = config.propDoor;
            hunterDoor = config.hunterDoor;
            propSpawn = config.propSpawn;
            hunterSpawn = config.hunterSpawn;
        }
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
            if (currentHunters == 0 && Random.value < 0.5f) //first player will have a 1/2 prob. to be a hunter (TODO: do it in a pre-lobby!)
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
        //Transform startPos = GetStartPosition();
        if (spawnHunter)
        {
            Transform startPos = hunterSpawn;
            currentHunters++;
            player = startPos != null
                ? Instantiate(hunterPrefab, startPos.position, startPos.rotation)
                : Instantiate(hunterPrefab);
            listOfConnections.Add(conn.address, Enums.PlayerType.Hunter);
        }
        else
        {
            Transform startPos = propSpawn;
            currentProps++;
            player = startPos != null
                ? Instantiate(propPrefab, startPos.position, startPos.rotation)
                : Instantiate(propPrefab);
            listOfConnections.Add(conn.address, Enums.PlayerType.Prop);
        }
        currentPlayers++;

        if (numPlayers == 0)
            player.GetComponent<PlayerController>().isAdmin = true;
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