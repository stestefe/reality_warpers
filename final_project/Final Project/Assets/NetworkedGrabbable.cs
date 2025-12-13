using Unity.Netcode;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

/// <summary>
/// Makes a grabbable object work across the network
/// Attach this to your grabbable object alongside NetworkObject and NetworkTransform
/// </summary>
public class NetworkedGrabbable : NetworkBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private Rigidbody rb;
    
    [SerializeField] private HandGrabInteractable handGrabInteractable;
    
    private bool isGrabbed = false;

    private void Awake()
    {
        if (grabbable == null)
            grabbable = GetComponent<Grabbable>();
        
        if (handGrabInteractable == null)
            handGrabInteractable = GetComponent<HandGrabInteractable>();
            
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Subscribe to grab events
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
        
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenInteractorViewAdded += HandleGrabbed;
            handGrabInteractable.WhenInteractorViewRemoved += HandleReleased;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
        
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenInteractorViewAdded -= HandleGrabbed;
            handGrabInteractable.WhenInteractorViewRemoved -= HandleReleased;
        }
    }

    private void HandlePointerEvent(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type == PointerEventType.Select)
        {
            OnGrabbed();
        }
        else if (pointerEvent.Type == PointerEventType.Unselect)
        {
            OnReleased();
        }
    }

    private void HandleGrabbed(IInteractorView interactor)
    {
        OnGrabbed();
    }

    private void HandleReleased(IInteractorView interactor)
    {
        OnReleased();
    }

    private void OnGrabbed()
    {
        if (!IsOwner)
        {
            RequestOwnershipServerRpc();
        }
        
        isGrabbed = true;
    }

    private void OnReleased()
    {
        isGrabbed = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default)
    {
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner && rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void Update()
    {
        if (IsOwner && rb != null)
        {
            rb.isKinematic = isGrabbed;
        }
    }
}