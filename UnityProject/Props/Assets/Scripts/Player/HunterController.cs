using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class HunterController : PlayerController
{
    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector3 attackRange;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int damage;
    [SerializeField] private float attackCooldown;
    [SerializeField] private float attackDuration;
    [SerializeField] private GameObject attackParticles;

    private float nextAttack;
    private float nextMove;


    public override void Update()
    {
        base.Update();

        if (attackParticles.activeSelf && Time.fixedTime >= nextMove)
            CmdSetParticles(false);
    }


    protected override void HandleMovement()
    {
        if (Time.fixedTime >= nextMove)
            base.HandleMovement();
    }
    protected override void HandleRotation()
    {
        if (Time.fixedTime >= nextMove)
            base.HandleRotation();
    }

    public override void Action(InputAction.CallbackContext context)
    {
        //Unity hace 3 llamadas: Iniciado, cancelado y terminado. Solo nos interesa la primera
        if (!context.started || !isLocalPlayer || isDead)
            return;
        base.Action(context);
    }
    public override void Fire(InputAction.CallbackContext context)
    {
        //Unity hace 3 llamadas: Iniciado, cancelado y terminado. Solo nos interesa la primera
        if (!context.started || !isLocalPlayer || Time.fixedTime < nextAttack || isDead)
            return;

        //plays the sound
        audioManager.PlayAudio(Enums.SoundType.hunterAttack);
        // sets the cooldown and shows it in UI
        menuScript.NewCooldown(Enums.CooldownType.Melee, attackCooldown);

        nextAttack = Time.fixedTime + attackCooldown;
        nextMove = Time.fixedTime + attackDuration;
        CmdSetParticles(true);

        Collider[] hitEnemies = Physics.OverlapBox(attackPoint.position, attackRange, Quaternion.identity, enemyLayers);
        bool playerFound = false;
        foreach (var hit in hitEnemies)
        {
            if (hit.CompareTag("Player"))
            {
                // get the player id
                uint playerId = hit.GetComponent<PlayerController>().GetPlayerId();
                CmdSendDamage(playerId, damage);

                //check if he is in range and visible
                if (Physics.Raycast(attackPoint.position, (hit.transform.position - attackPoint.position), out RaycastHit rayHit))
                {
                    if (rayHit.transform.name.Equals(hit.transform.name))
                    {
                        Debug.Log("Player found, visible and in range!");
                        playerFound = true;
                        break;
                    }
                }
            }
            else
            {
                //is a prop
            }
        }
        //if no player has been found, then dmg the hunter
        if (!playerFound)
        {
            CmdSendDamage(this.playerId, damage);
        }
    }

    [Command]
    void CmdSendDamage(uint playerId, int damage)
    {
        NetworkClient.spawned[playerId].GetComponent<PlayerController>().RpcSendDamage(playerId, damage);
    }
    [Command]
    void CmdSetParticles(bool active)
    {
        RpcSetParticles(active);
    }

    [ClientRpc]
    void RpcSetParticles(bool active)
    {
        attackParticles.SetActive(active);
    }
}
