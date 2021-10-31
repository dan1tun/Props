using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class HunterController : PlayerController
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector3 attackRange;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int damage;
    [SerializeField] private float attackCooldown;

    private float nextAttack;



    public override void Action(InputAction.CallbackContext context)
    {
        //Unity hace 3 llamadas: Iniciado, cancelado y terminado. Solo nos interesa la primera
        if (!context.started || !isLocalPlayer)
            return;
        base.Action(context);
    }
    public override void Fire(InputAction.CallbackContext context)
    {
        //Unity hace 3 llamadas: Iniciado, cancelado y terminado. Solo nos interesa la primera
        if (!context.started || !isLocalPlayer || Time.time < nextAttack)
            return;

        nextAttack = Time.time + attackCooldown;

        Debug.Log("Fire pressed"); 
        Collider[] hitEnemies = Physics.OverlapBox(attackPoint.position, attackRange, Quaternion.identity, enemyLayers);
        foreach (var hit in hitEnemies)
        {
            if (!hit.CompareTag("Player"))
                return;
            Debug.Log("HIT! " + hit.name);
            // get the player id
            uint playerId = hit.GetComponent<PlayerController>().GetPlayerId();
            CmdSendDamage(playerId, damage);
        }
    }

    [Command]
    void CmdSendDamage(uint playerId, int damage)
    {
        NetworkClient.spawned[playerId].GetComponent<PlayerController>().RpcSendDamage(playerId, damage);
    }
}
