using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnGameObject : MonoBehaviour
{
    public GameObject gameObject1Prefab;
    public int maxObjects = 10;
    public Vector2 planeSize = new Vector2(5f, 5f);
    // public float padding = 1f;
    public List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        InvokeRepeating(nameof(SpawnObject), 1f, 2f);
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
        float z = Random.Range(-halfZ, halfZ);
        Vector3 spawnPos = new Vector3(x, 0f, z);

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

