using UnityEngine;
using Unity.Netcode;

public class NetworkConnect : MonoBehaviour
{
    [SerializeField] private GameObject headRepresentationPrefab;
    
    private GameObject localHeadRepresentation;

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        SpawnLocalHeadRepresentation();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        
        // Wait for connection, then spawn
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only spawn for local client
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SpawnLocalHeadRepresentation();
        }
    }

    private void SpawnLocalHeadRepresentation()
    {
        if (localHeadRepresentation != null) return;

        // Spawn the networked head representation
        localHeadRepresentation = Instantiate(headRepresentationPrefab);
        NetworkObject netObj = localHeadRepresentation.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
        
        Debug.Log("Spawned head representation for local player");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
