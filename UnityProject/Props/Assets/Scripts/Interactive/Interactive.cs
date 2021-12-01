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
    [SerializeField] private Vector3 addPosition;
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

    public void Action(float newBlockTime = 0, bool fromButton = false)
    {
        Debug.Log("Action");
        if (Time.fixedTime < blockedUntil)
            return;

        // add the cooldown
        if (newBlockTime == 0)
            newBlockTime = cooldown;
        blockedUntil = Time.fixedTime + newBlockTime;

        switch (type)
        {
            case Enums.InteractiveType.Button:
                UseButton();
                break;
            case Enums.InteractiveType.Door:
                MoveDoor(fromButton);
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
            target.GetComponent<Interactive>().Action(blockTarget ? blockTime : 0, true);
        }
    }

    public void MoveDoor(bool fromButton)
    {
        Debug.Log("MoveDoor called");

        // si viene de un botón y está cerrado, no movemos la puerta
        if (fromButton && !this.opened)
            return;

        if (opened)
        {
            opened = false;
            transform.position += addPosition;
        }
        else
        {
            opened = true;
            transform.position -= addPosition;
        }
    }

    public Enums.SoundType GetAudio()
    {
        if (opened)
            return Enums.SoundType.DoorClose;
        else
            return Enums.SoundType.DoorOpen;
    }
}
