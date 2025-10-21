using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SpawnGameObject : MonoBehaviour
{
    public GameObject gameObject1Prefab;
    public int maxObjects = 25;
    public Vector2 planeSize = new Vector2(5f, 5f);
    // public float padding = 1f;
    public List<GameObject> spawnedObjects = new List<GameObject>();

    public GameObject cart;

    void Start()
    {
        InvokeRepeating(nameof(SpawnObject), 0f, 0.8f);
    }

    void SpawnObject()
    {
        if (spawnedObjects.Count >= maxObjects)
            return;

        float halfX = planeSize.x / 2f; // - padding;
        float halfZ = planeSize.y / 2f; //- padding;

        if (halfX <= 0 || halfZ <= 0)
        {
            return;
        }

        float x = Random.Range(-halfX, halfX);
        float z = Random.Range(-halfZ - 1, halfZ);
        Vector3 spawnPos = new Vector3(x, 0f, z + 0.9f);
        if(Vector3.Distance(cart.transform.position, spawnPos) < 0.98)
        {
            return;
        }

        GameObject obj = Instantiate(gameObject1Prefab, spawnPos, Quaternion.identity);
        spawnedObjects.Add(obj);
    }

    public void RemoveFromList(GameObject obj)
    {
        spawnedObjects.Remove(obj);
    }

    public void DeactivateFlowers()
    {
        foreach (GameObject flower in spawnedObjects)
        {
            flower.SetActive(false);
        }
    }
    
    public void ActivateFlowers()
    {
        foreach(GameObject flower in spawnedObjects)
        {
            flower.SetActive(true);
        }
    }
}

