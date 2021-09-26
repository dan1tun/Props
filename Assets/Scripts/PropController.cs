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

                //lo escondemos en los demás clientes
                RpcHideBody(this.playerId);

                // enviamos solicitud al servidor para replicarlo el nuevo cuerpo
                //CmdChangeBody(obj.name)
                CmdChangeBody(obj.name, this.playerId);

                break;
            }
        }

        if (continueAction)
            base.Action(context);
    }

    [Command]
    private void CmdChangeBody(string objId, uint parentId)
    {
        //clonamos el objeto
        GameObject obj = GameObject.Find(objId);
        Transform parent = NetworkServer.spawned[parentId].gameObject.transform.Find("NewBody");
        Debug.Log("Parent: " + parent.name);
        GameObject newObject = Instantiate(propTest, parent, false);
        newObject.transform.localPosition = Vector3.zero;
        newObject.tag = "NewBody";
        newObject.GetComponent<ParentHelper>().parentNetId = this.netId;
        NetworkServer.Spawn(newObject, connectionToClient);

        //sincronizamos las propiedades en todos los clientes
        //RpcHideBody(objId);
    }

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
}
