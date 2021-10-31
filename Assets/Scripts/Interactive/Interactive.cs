using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Interactive : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Enums.InteractiveType type;
    [SerializeField] private GameObject target;
    [SerializeField] private float cooldown;


    [Header("Door")]
    [SerializeField] private Vector3 finalPosition;
    [SerializeField] private bool opened = true;
    private Vector3 initialPosition;

    [Header("Button")]
    [SerializeField] private bool blockTarget;
    [SerializeField] private float blockTime;
    [SerializeField] private bool forceClose = false;


    //GLOBAL
    private float blockedUntil;

    private void Start()
    {
        initialPosition = transform.position;
    }

    public void Action(float newBlockTime = 0, bool close = false)
    {
        Debug.Log("Action");
        if (Time.fixedTime < blockedUntil)
            return;

        // add the cooldown
        if (newBlockTime == 0)
            newBlockTime = cooldown;
        blockedUntil = Time.fixedTime + newBlockTime;

        if (close)
            this.opened = true;

        switch (type)
        {
            case Enums.InteractiveType.Button:
                UseButton();
                break;
            case Enums.InteractiveType.Door:
                MoveDoor();
                break;
            default:
                break;
        }
    }



    public void UseButton()
    {
        Debug.Log("UseButton called");
        if (target)
        {
            target.GetComponent<Interactive>().Action(blockTarget ? blockTime : 0, forceClose);
        }
    }

    public void MoveDoor()
    {
        Debug.Log("MoveDoor called");
        if (opened)
        {
            opened = false;
            transform.position = finalPosition;
        }
        else
        {
            opened = true;
            transform.position = initialPosition;
        }
    }
}
