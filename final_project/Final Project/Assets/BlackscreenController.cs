using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BlackscreenController : NetworkBehaviour
{
    public GameObject blackScreenObject;
    
    public float activeDuration = 10f;
    
    private Coroutine deactivationCoroutine;

    public NetworkVariable<bool> isBlackscreenActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        isBlackscreenActive.OnValueChanged += OnBlackscreenStateChanged;
        
        if (blackScreenObject != null)
        {
            blackScreenObject.SetActive(isBlackscreenActive.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isBlackscreenActive.OnValueChanged -= OnBlackscreenStateChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateBlackscreenServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (IsServer)
        {
            Debug.Log($"Server received blackscreen activation request from client {serverRpcParams.Receive.SenderClientId}");
            ActivateBlackscreen();
        }
    }

    private void ActivateBlackscreen()
    {
        if (!IsServer) return;

        if (deactivationCoroutine != null)
        {
            StopCoroutine(deactivationCoroutine);
        }

        isBlackscreenActive.Value = true;
        
        if (blackScreenObject != null)
        {
            blackScreenObject.SetActive(true);
        }

        Debug.Log($"Server activated blackscreen for {activeDuration} seconds");

        deactivationCoroutine = StartCoroutine(DeactivateAfterDuration());
    }

    private IEnumerator DeactivateAfterDuration()
    {
        yield return new WaitForSeconds(activeDuration);

        isBlackscreenActive.Value = false;
        
        if (blackScreenObject != null)
        {
            blackScreenObject.SetActive(false);
        }

        Debug.Log("Server deactivated blackscreen");
        deactivationCoroutine = null;
    }

    private void OnBlackscreenStateChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"Blackscreen state changed to: {newValue}");
        
        if (blackScreenObject != null)
        {
            blackScreenObject.SetActive(newValue);
        }
    }
}