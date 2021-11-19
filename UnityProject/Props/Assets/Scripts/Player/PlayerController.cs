using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed = 0;
    [SerializeField] private float detectionRange = 7;
    [SerializeField] private float actionDistance;
    [SerializeField] private int health;
    [SerializeField] private GameObject virtualCamera, mainCameraObject;

    [HideInInspector] public MenuScript menuScript;
    [HideInInspector, SyncVar] public bool isAdmin;
    [HideInInspector, SyncVar(hook = "OnGameStarted")] public bool gameStarted;

    private int maxHealth;
    private Vector3 moveVector = new Vector3();
    private Vector3 lookVector = new Vector3();
    protected List<GameObject> inRange = new List<GameObject>();
    private bool fireStarted = false;

    [SyncVar]
    protected uint playerId;

    #region Components
    private Rigidbody rigidBody = new Rigidbody();
    private PlayerInput playerInput;
    private Camera mainCamera;
    private CustomNetworkManager networkManager;

    #endregion

    public uint GetPlayerId()
    {
        return this.playerId;
    }

    public override void OnStartClient()
    {
        // Cuando ha sido spawneado (clientside)
        playerId = this.netId;
        maxHealth = health;
    }

    //Enable the input only for the current player
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = true;

        //setup camera for this player
        mainCameraObject.SetActive(true);
        virtualCamera.SetActive(true);
        mainCameraObject.transform.parent = null;
        virtualCamera.transform.parent = null;
        mainCamera = Camera.main;
    }

    [Client]
    public virtual void Start()
    {
        rigidBody = GetComponent<Rigidbody>();


        GameObject networkObj = GameObject.Find("NetworkManager");
        networkManager = networkObj.GetComponent<CustomNetworkManager>();
        //TODO: esto es feo. Mirar la forma de hacerlo en el lado del servidor (customnetworkmanager => OnServerAddPlayer / OnClientConnect
        if (menuScript == null)
        {
            Debug.Log("Menu was null");
            menuScript = networkObj.GetComponent<MenuScript>();
        }
        menuScript.playerController = this;

        // si el numero de jugadores es 1, soy el admin. Habilito el boton para empezar la partida
        if (networkManager.numPlayers == 1)
        {
            isAdmin = true;
            menuScript.ShowAdminScreen();
        }
    }

    [Client]
    public virtual void Update()
    {

    }

    [Client]
    private void FixedUpdate()
    {
        if (!hasAuthority)
            return;

        HandleMovement();
        HandleRotation();
    }

    public void KillPlayer()
    {
        playerInput.DeactivateInput();
        menuScript.ShowDeadScreen();
    }

    #region INPUT HANDLERS
    //La mayoría de inputs los controlaremos en las clases heredadas del Prop y del Hunter, o en otros apartados de este mismo Script.
    //El input debería ser algo "tonto": simplemente indica un input, pero no lo que se debe hacer con él

    public void Look(InputAction.CallbackContext context) => lookVector = context.ReadValue<Vector2>();

    public void Move(InputAction.CallbackContext context) => moveVector = context.ReadValue<Vector2>();

    public void OpenMenu(InputAction.CallbackContext context) => menuScript.menuOpened = !menuScript.menuOpened;

    public virtual void Fire(InputAction.CallbackContext context) => fireStarted = true;

    public virtual void Action(InputAction.CallbackContext context)
    {
        foreach (GameObject obj in inRange)
        {
            if (obj)
            {
                Debug.Log($"Object: " + obj.name);
                if (obj.CompareTag("Interactive"))
                {
                    CmdAction(obj.GetComponent<Interactive>().netId);
                }
            }

        }
    }

    #endregion


    #region PhysicsActions

    protected virtual void HandleMovement()
    {
        if (moveVector != Vector3.zero && !menuScript.menuOpened)
        {
            Vector3 direction = new Vector3(moveVector.x, 0, moveVector.y);
            rigidBody.MovePosition(transform.position + direction * Time.deltaTime * speed);
        }
    }

    protected virtual void HandleRotation()
    {
        //ignore input 0
        if (lookVector == Vector3.zero || menuScript.menuOpened)
            return;

        // hacemos un trazado para detectar el punto donde está el ratón en el suelo
        if (Physics.Raycast(mainCamera.ScreenPointToRay(lookVector), out RaycastHit hit, 100, LayerMask.GetMask("Floor")))
        {
            // Miramos hacia el punto
            transform.LookAt(hit.point);

            // Alteramos la rotación X y Z del objeto (para que no vuelque) 
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;

            // Cambiamos la rotación del objeto a la nueva
            transform.rotation = Quaternion.Euler(eulerAngles);
        }
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactive"))
        {
            other.GetComponent<Outline>().enabled = true;
        }
        inRange.Add(other.gameObject);
    }
    public virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactive"))
        {
            other.GetComponent<Outline>().enabled = false;
            inRange.Remove(other.gameObject);
        }
        inRange.Remove(other.gameObject);
    }

    #endregion

    #region External callouts



    /// <summary>
    /// Damages the player.
    /// </summary>
    /// <param name="damage">Quantity of damage</param>
    [ClientRpc]
    public void RpcSendDamage(uint playerId, int damage)
    {
        PlayerController controller = NetworkClient.spawned[playerId].GetComponent<PlayerController>();

        controller.health -= damage;
        if (controller.health <= 0)
        {
            controller.health = 0; // para que no baje de 0

            //TODO: deberíamos matar al jugador. Por ahora, desconectamos su input
            if (controller.playerInput)
                controller.KillPlayer();
        }
    }
    #endregion

    #region Mirror
    [Command]
    private void CmdAction(uint netId)
    {
        Debug.Log("CmdAction id: " + netId);
        RpcAction(netId);
    }
    [ClientRpc]
    private void RpcAction(uint netId)
    {
        Debug.Log("RpcAction id: " + netId);
        NetworkClient.spawned[netId].gameObject.GetComponent<Interactive>().Action();
    }
    #endregion



    #region Game states

    /// <summary>
    /// Starts game. Initial call (only admin)
    /// </summary>
    public void StartGame()
    {
        foreach (var player in NetworkServer.connections.Values)
        {
            player.identity.gameObject.GetComponent<PlayerController>().gameStarted = true;
        }
    }

    void OnGameStarted(bool oldValue, bool newValue)
    {
        menuScript.NewPhase(Enums.RoundType.Starting, networkManager.initialTime);
    }

    /// <summary>
    /// Starts preround. Client call (destroys prop door, changes UI info)
    /// </summary>
    public void StartPreround()
    {
        menuScript.NewPhase(Enums.RoundType.Preround, networkManager.preRoundTime);
        GameObject.Destroy(networkManager.propDoor);
    }

    /// <summary>
    /// Starts round. Client call (destroys hunter door, changes UI info)
    /// </summary>
    public void StartRound()
    {
        menuScript.NewPhase(Enums.RoundType.HideAndSeek, networkManager.roundTime);
        GameObject.Destroy(networkManager.hunterDoor);
    }
    /// <summary>
    /// Starts flight. Client call (to be decided, changes UI info)
    /// </summary>
    public void StartFlight()
    {
        //TODO: Start the scape phase (to be decided)
        //por ahora, muestro una ventana para salir
        menuScript.NewPhase(Enums.RoundType.HideAndSeek, networkManager.roundTime);
    }


    public void EndGame()
    {
        menuScript.SwitchPanels(endPanelActive: true);
    }

    #endregion
}
