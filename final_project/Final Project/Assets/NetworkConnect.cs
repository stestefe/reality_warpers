using UnityEngine;
using Unity.Netcode;

public class NetworkConnect : MonoBehaviour
{
    [SerializeField] private GameObject hostHeadRepresentationPrefab;
    [SerializeField] private GameObject clientHeadRepresentationPrefab;

    private GameObject localHeadRepresentation;

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        // SpawnLocalPlayerObject(true);
    }

    public void StartClient()
    {
        // NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.StartClient();
        // SpawnLocalPlayerObject(false);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SpawnLocalPlayerObject(false);
        }
    }

    private void SpawnLocalPlayerObject(bool isHost)
    {
        if (localHeadRepresentation != null)
            return;

        GameObject prefab = isHost ? hostHeadRepresentationPrefab : clientHeadRepresentationPrefab;
        Debug.Log($"HOSTS: {isHost}");
        localHeadRepresentation = Instantiate(prefab);

        NetworkObject netObj = localHeadRepresentation.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);

        Debug.Log("spawned head object for local client");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
