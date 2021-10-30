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
        Debug.Log("Player start");
        rigidBody = GetComponent<Rigidbody>();

        //TODO: esto es feo. Mirar la forma de hacerlo en el lado del servidor (customnetworkmanager => OnServerAddPlayer / OnClientConnect
        if (menuScript == null)
        {
            menuScript = GameObject.Find("NetworkManager").GetComponent<MenuScript>();
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

    public virtual void Action(InputAction.CallbackContext context) { }

    #endregion


    #region PhysicsActions

    private void HandleMovement()
    {
        if (moveVector != Vector3.zero && !menuScript.menuOpened)
        {
            Vector3 direction = new Vector3(moveVector.x, 0, moveVector.y);
            rigidBody.MovePosition(transform.position + direction * Time.deltaTime * speed);
        }
    }

    private void HandleRotation()
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
}
