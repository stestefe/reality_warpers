using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Flower"))
        {
            Destroy(other.gameObject);

        //     GameObject spawner = GameObject.FindObjectOfType<SpawnGameObject>()?.gameObject;
        //     if (spawner != null)
        //     {
        //         spawner.GetComponent<SpawnGameObject>().RemoveFromList(other.gameObject);
        //     }
        }
    }
}