using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Vector3 targetVector;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float cooldownForPlayer = 1;
    [SerializeField] private List<GameObject> pairedPortals;

    private float nextTime;
    // Start is called before the first frame update
    void Start()
    {
        if (targetTransform != null)
            targetVector = targetTransform.position;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (Time.fixedTime >= nextTime && other.CompareTag("Player"))
        {
            other.transform.position = targetVector;
            nextTime = Time.fixedTime + cooldownForPlayer;
            foreach (var portal in pairedPortals)
            {
                portal.GetComponent<Portal>().AddCooldown(cooldownForPlayer);
            }
        }
    }

    public void AddCooldown(float cooldown) => nextTime = Time.fixedTime + cooldown;
}
