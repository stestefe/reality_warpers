using UnityEngine;
using Unity.Netcode;

public class NetworkObjectSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnData
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
    }

    [SerializeField] private SpawnData[] spawnList;

    public void SpawnAll()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        foreach (var data in spawnList)
        {
            GameObject obj = Instantiate(
                data.prefab,
                data.position,
                data.rotation
            );

            obj.GetComponent<NetworkObject>().Spawn();
        }
    }
}
