using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ParentHelper : NetworkBehaviour
{
    [SyncVar]
    public uint parentNetId;

    public override void OnStartClient()
    {
        // When we are spawned on the client,
        // find the parent object using its ID,
        // and set it to be our transform's parent.
        if (parentNetId == 0)
            return;

        Debug.Log("Parenting prop " + this.name + " with parentId: " + parentNetId);
        GameObject parentPlayer = NetworkClient.spawned[parentNetId].gameObject;
        transform.SetParent(parentPlayer.transform.Find("NewBody"));
    }
}
