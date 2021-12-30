using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailerCameraMovement : MonoBehaviour
{
    public float moveSpeedX, moveSpeedY, moveSpeedZ, delay;

    private float initTime;
    // Start is called before the first frame update
    void Start()
    {
        initTime = Time.fixedTime + delay;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.fixedTime > initTime)
            transform.position +=new Vector3(moveSpeedX * Time.deltaTime, moveSpeedY * Time.deltaTime, moveSpeedZ * Time.deltaTime );
    }

    
}
