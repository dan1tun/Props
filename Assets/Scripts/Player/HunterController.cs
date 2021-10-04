using System.Collections;
using System.Collections.Generic;
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
            Debug.Log("HIT! " + hit.name);
            if (hit.GetComponent<PlayerController>().Damage(damage))
                Debug.Log("He is dead!");
        }
    }
}
