using UnityEngine;
using Unity.Netcode;

public class TriggerSync : NetworkBehaviour
{
    [SerializeField] private string targetTag = "PhysicalPlayer";
    public GameObject clientObjectToEnable;

    private NetworkVariable<bool> isTriggered = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private void OnEnable()
    {
        isTriggered.OnValueChanged += OnTriggerStateChanged;
    }

    private void OnDisable()
    {
        isTriggered.OnValueChanged -= OnTriggerStateChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"hello {IsServer}");
        if (!IsServer) return;
        
        if (other.CompareTag(targetTag))
        {
            Debug.Log("entered ");
            isTriggered.Value = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        
        if (other.CompareTag(targetTag))
        {
            isTriggered.Value = false;
        }
    }

    private void OnTriggerStateChanged(bool oldValue, bool newValue)
    {
        if (clientObjectToEnable != null)
        {
            clientObjectToEnable.SetActive(newValue);
        }
    }

    public bool IsCurrentlyTriggered()
    {
        return isTriggered.Value;
    }
}