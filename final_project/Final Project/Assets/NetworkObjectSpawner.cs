using UnityEngine;
using Unity.Netcode;

public class NetworkObjectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] sharedPrefabs;

    public void SpawnAll()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        foreach (var prefab in sharedPrefabs)
        {
            GameObject obj = Instantiate(prefab);
            obj.GetComponent<NetworkObject>().Spawn();
        }
    }
}
