using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class ControllerBeam : NetworkBehaviour
{
    public Transform controller;
    public Vector3 beamScale = new Vector3(0.05f, 0.05f, 1f);
    public Material beamMaterial;
    public InputActionProperty triggerAction;
    public InputActionProperty buttonAction; // New: Button input
    public string targetTag = "Player";
    public GameObject passThroughPlane;

    private GameObject beamCube;
    private BoxCollider beamCollider;

    public NetworkVariable<bool> isColliding = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );
    
    public NetworkVariable<bool> buttonTriggered = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );

    void Start()
    {
        beamCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        beamCube.transform.SetParent(controller, false);
        beamCube.transform.localRotation = Quaternion.identity;
        beamCube.transform.localScale = beamScale;
        beamCube.transform.localPosition = new Vector3(0, 0, beamScale.z / 2f);

        beamCollider = beamCube.GetComponent<BoxCollider>();
        beamCollider.isTrigger = true;

        if (beamMaterial != null)
            beamCube.GetComponent<Renderer>().material = beamMaterial;

        beamCube.SetActive(false);

        triggerAction.action.Enable();
        buttonAction.action.Enable();

        BeamCollisionDetector detector = beamCube.AddComponent<BeamCollisionDetector>();
        detector.Initialize(this, targetTag);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        isColliding.OnValueChanged += OnCollidingChanged;
        buttonTriggered.OnValueChanged += OnButtonTriggeredChanged; // New
        
        OnCollidingChanged(false, isColliding.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isColliding.OnValueChanged -= OnCollidingChanged;
        buttonTriggered.OnValueChanged -= OnButtonTriggeredChanged; // New
    }

    void Update()
    {
        if (!IsOwner) return;

        bool triggerPressed = triggerAction.action.ReadValue<float>() > 0.1f;
        beamCube.SetActive(triggerPressed);

        if (triggerPressed)
        {
            beamCube.transform.localRotation = Quaternion.identity;
            beamCube.transform.localScale = beamScale;
            beamCube.transform.localPosition = new Vector3(0, 0, beamScale.z / 2f);
        }

        if (buttonAction.action.WasPressedThisFrame() && isColliding.Value)
        {
            buttonTriggered.Value = true;
            Debug.Log("Button pressed while colliding!");
        }
    }

    public void OnBeamTriggerEnter(Collider other)
    {
        if (IsOwner)
        {
            isColliding.Value = true;
            passThroughPlane.SetActive(true);
            Debug.Log($"Beam colliding with: {other.gameObject.name}");
        }
    }

    public void OnBeamTriggerExit(Collider other)
    {
        if (IsOwner)
        {
            isColliding.Value = false;
            passThroughPlane.SetActive(false);
            Debug.Log($"Beam stopped colliding with: {other.gameObject.name}");
        }
    }

    private void OnCollidingChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"Collision state changed: {newValue}");
    }

    private void OnButtonTriggeredChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"Button trigger state changed: {newValue}");
    }

    public bool IsColliding => isColliding.Value;
}

public class BeamCollisionDetector : MonoBehaviour
{
    private ControllerBeam parentBeam;
    private string targetTag;

    public void Initialize(ControllerBeam parent, string tag)
    {
        parentBeam = parent;
        targetTag = tag;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
        {
            parentBeam?.OnBeamTriggerEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
        {
            parentBeam?.OnBeamTriggerExit(other);
        }
    }
}