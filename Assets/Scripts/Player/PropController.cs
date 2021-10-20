using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PropController : PlayerController
{
    [SerializeField] private GameObject baseBody, newBody;
    [SerializeField] private float afkTime = 15, afkTimeBetweenChecks = 2, distanceToCheck = 2;
    public GameObject propIndicator;

    private Vector3 lastPosition;
    private float nextCheckTime, currentAfkTime;
    private bool inPropMode;

    public override void Start()
    {
        base.Start();
    }

    public override void Update()
    {
        base.Update();

        if (inPropMode)
            CheckMovement();
    }

    private void CheckMovement()
    {
       if (Time.time >= nextCheckTime)
        {
            Debug.Log("Checking...");
            nextCheckTime = Time.time + afkTimeBetweenChecks;
            Vector3 currentPosition = transform.position;

            //si se ha movido lo suficiente, reiniciamos el tiempo
            if (Vector3.Distance(lastPosition, currentPosition) >= distanceToCheck)
            {
                currentAfkTime = 0;
                CmdSetAfk(this.playerId, false);
                //propIndicator.SetActive(false);
            }
            else if (currentAfkTime > afkTime)
            {
                Debug.Log("Time excedeed! CurrentAfkTime: " + currentAfkTime + " // afkTime: " + afkTime);
                CmdSetAfk(this.playerId, true);
                //propIndicator.SetActive(true);
            }
            lastPosition = currentPosition;


            currentAfkTime += afkTimeBetweenChecks;
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (!hasAuthority)
            return;
        base.OnTriggerEnter(other);

        if (other.CompareTag("Prop"))
            other.GetComponent<Outline>().enabled = true;
    }
    public override void OnTriggerExit(Collider other)
    {
        if (!hasAuthority)
            return;
        base.OnTriggerExit(other);

        if (other.CompareTag("Prop"))
            other.GetComponent<Outline>().enabled = false;
    }


    public override void Action(InputAction.CallbackContext context)
    {
        //Unity hace 3 llamadas: Iniciado, cancelado y terminado. Solo nos interesa la primera
        if (!context.started || !isLocalPlayer)
            return;

        bool continueAction = true;

        //buscamos si tenemos un prop en rango para transformarnos
        foreach (GameObject obj in inRange)
        {
            if (obj.CompareTag("Prop"))
            {
                Debug.Log("Prop found");
                continueAction = false;

                //quitamos el outline del objeto
                obj.GetComponent<Outline>().enabled = false;


                // enviamos solicitud al servidor para replicarlo el nuevo cuerpo
                //CmdChangeBody(obj.name)
                CmdChangeBody(obj.name, this.playerId);

                this.inPropMode = true;

                break;
            }
        }

        if (continueAction)
            base.Action(context);
    }

    /// <summary>
    /// Prop is selected. Instantiate on server, then clone on clients
    /// </summary>
    /// <param name="objId">Name of the object</param>
    /// <param name="parentId">NetID of the player</param>
    [Command]
    private void CmdChangeBody(string objId, uint parentId)
    {
        //Hacemos el cambio de cuerpo en los demás clientes
        RpcHideBody(parentId);

        //clonamos el objeto
        RpcClone(objId, parentId);
    }

    /// <summary>
    /// Hide the baseBody of the player, and show the newBody
    /// </summary>
    /// <param name="id">NetId of the player</param>
    [ClientRpc]
    private void RpcHideBody(uint id)
    {
        Transform target = NetworkClient.spawned[id].transform;
        target.transform.Find("NewBody").gameObject.SetActive(true);
        target.transform.Find("BaseBody").gameObject.SetActive(false);
    }
    /// <summary>
    /// Clone the prop locally
    /// </summary>
    /// <param name="objId">Prop name</param>
    /// <param name="parentId">Player NetID</param>
    [ClientRpc]
    private void RpcClone(string objId, uint parentId)
    {
        Transform parent = NetworkClient.spawned[parentId].gameObject.transform.Find("NewBody");

        //primero eliminamos lo que pueda tener antes
        foreach (Transform child in parent)
            GameObject.Destroy(child.gameObject);
        
        GameObject prop = GameObject.Find(objId);
        GameObject newObject = Instantiate(prop, parent, false);
        newObject.tag = "NewBody";
        newObject.transform.localPosition = new Vector3(0f, prop.transform.position.y - this.transform.position.y, 0f);
        newObject.transform.localRotation = Quaternion.identity;
    }



    /// <summary>
    /// Sends the clients information about the afk state of the prop player
    /// </summary>
    /// <param name="playerId">Prop player</param>
    /// <param name="showAfk">Afk state</param>
    [Command]
    private void CmdSetAfk(uint playerId, bool showAfk)
    {
        RpcShowAfk(playerId, showAfk);
    }
    /// <summary>
    /// Applies the afk state to the prop player
    /// </summary>
    /// <param name="playerId">Prop player</param>
    /// <param name="showAfk">Afk state</param>
    [ClientRpc]
    private void RpcShowAfk(uint playerId, bool showAfk)
    {
        NetworkClient.spawned[playerId].GetComponent<PropController>().propIndicator.SetActive(showAfk);
    }
}
