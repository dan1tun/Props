using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PropController : PlayerController
{
    [SerializeField] private GameObject baseBody;
    [SerializeField] private GameObject newBody;
    public GameObject propTest;

    public override void Start()
    {
        base.Start();
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
        //lo escondemos en los demás clientes
        RpcHideBody(parentId);

        //Recuperamos el jugador
        Transform parent = NetworkServer.spawned[parentId].gameObject.transform.Find("NewBody");

        //clonamos el objeto
        GameObject obj = GameObject.Find(objId);
        GameObject newObject = Instantiate(propTest, parent, false);
        newObject.tag = "NewBody";
        newObject.GetComponent<ParentHelper>().parentNetId = parentId;

        //instanciamos en servidor
        NetworkServer.Spawn(newObject, connectionToClient);

        //reiniciamos la vista en los clientes
        RpcResetLocalPositionOfNewBody(parentId);
    }

    /// <summary>
    /// Hide the baseBody of the player, and show the newBody
    /// </summary>
    /// <param name="id">NetId of the player</param>
    [ClientRpc]
    private void RpcHideBody(uint id)
    {
        //GameObject obj = GameObject.Find(objId);
        //clonamos el objeto
        //GameObject newObject = Instantiate(propTest, this.transform.position, obj.transform.rotation, parent: newBody.transform);
        //newObject.tag = "NewBody";
        //newObject.GetComponent<ParentHelper>().parentNetId = this.netId;
        Transform target = NetworkClient.spawned[id].transform;
        target.transform.Find("NewBody").gameObject.SetActive(true);
        target.transform.Find("BaseBody").gameObject.SetActive(false);
    }
    /// <summary>
    /// Reset the position of the newBody and sets its rotation equal to its parent
    /// </summary>
    /// <param name="id"></param>
    [ClientRpc]
    private void RpcResetLocalPositionOfNewBody(uint id)
    {
        Transform player = NetworkClient.spawned[id].transform;
        Transform newBody = player.transform.Find("NewBody");

        for (int i = 0; i < newBody.childCount; i++)
        {
            Transform child = newBody.GetChild(i);
            child.localPosition = Vector3.zero;
            child.rotation = player.rotation;
        }
    }
}
