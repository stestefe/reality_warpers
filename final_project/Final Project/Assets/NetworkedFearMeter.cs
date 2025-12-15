using Unity.Netcode;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class NetworkedFearMeter : NetworkBehaviour
{
    [SerializeField] private float maxFear = 100f;
    [SerializeField] private float fearPerDetection = 10f;
    
    [SerializeField] private Vector3 meterPosition = new Vector3(-1.54f, 1.1f, 3.31f);
    [SerializeField] private float meterHeight = 2f;
    [SerializeField] private float meterRadius = 0.3f;
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color fillColor = Color.red;

    [SerializeField] private DualTrapezoPrismWireframe frustumDetector;
    
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float movementThreshold = 0.01f;
    
    [SerializeField] private bool ignoreGrabbedObjects = true;
    
    private NetworkVariable<float> currentFear = new NetworkVariable<float>(
        0f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    
    private GameObject meterContainer;
    private GameObject fillCylinder;
    private Material fillMaterial;
    private Vector3 fillBaseScale;
    
    private System.Collections.Generic.Dictionary<GameObject, Vector3> objectLastPositions = 
        new System.Collections.Generic.Dictionary<GameObject, Vector3>();

    private void Start()
    {
        CreateFearMeter();
        
        currentFear.OnValueChanged += OnFearValueChanged;
        
        UpdateMeterVisual(currentFear.Value);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[FearMeter] Initialized. IsServer: {IsServer}, IsClient: {IsClient}");
        }
    }

    private void CreateFearMeter()
    {
        meterContainer = new GameObject("FearMeter");
        meterContainer.transform.position = meterPosition;
        
        GameObject bgCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bgCylinder.name = "Background";
        bgCylinder.transform.SetParent(meterContainer.transform);
        bgCylinder.transform.localPosition = Vector3.zero;
        bgCylinder.transform.localScale = new Vector3(meterRadius * 2, meterHeight / 2, meterRadius * 2);
        
        Renderer bgRenderer = bgCylinder.GetComponent<Renderer>();
        Shader bgShader = Shader.Find("Unlit/Color");
        if (bgShader == null) bgShader = Shader.Find("Standard");
        Material bgMaterial = new Material(bgShader);
        bgMaterial.color = emptyColor;
        bgRenderer.material = bgMaterial;
        
        Destroy(bgCylinder.GetComponent<Collider>());
        
        fillCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fillCylinder.name = "Fill";
        fillCylinder.transform.SetParent(meterContainer.transform);
        fillCylinder.transform.localPosition = new Vector3(0, -meterHeight / 2, 0);
        fillBaseScale = new Vector3(meterRadius * 1.8f + 0.2f, 0, meterRadius * 1.8f + 0.2f);
        fillCylinder.transform.localScale = fillBaseScale;
        
        Renderer fillRenderer = fillCylinder.GetComponent<Renderer>();
        Shader fillShader = Shader.Find("Unlit/Color");
        if (fillShader == null) fillShader = Shader.Find("Standard");
        fillMaterial = new Material(fillShader);
        fillMaterial.color = fillColor;
        fillMaterial.EnableKeyword("_EMISSION");
        fillMaterial.SetColor("_EmissionColor", fillColor * 0.5f);
        fillRenderer.material = fillMaterial;
        
        Destroy(fillCylinder.GetComponent<Collider>());
        
        if (enableDebugLogs)
        {
            Debug.Log($"[FearMeter] 3D meter created at position {meterPosition}");
        }
    }

    private void Update()
    {
        if (!IsServer || frustumDetector == null) return;
        
        CheckForFearInducingMovement();
    }

    private void CheckForFearInducingMovement()
    {
        var objectsInFrustum1 = frustumDetector.GetObjectsInFrustum1();
        var objectsInFrustum2 = frustumDetector.GetObjectsInFrustum2();
        
        if (enableDebugLogs && (objectsInFrustum1.Count > 0 || objectsInFrustum2.Count > 0))
        {
            Debug.Log($"[FearMeter] Checking frustums - F1: {objectsInFrustum1.Count} objects, F2: {objectsInFrustum2.Count} objects");
        }
        
        CheckFrustumObjects(objectsInFrustum1, "Frustum1");
        CheckFrustumObjects(objectsInFrustum2, "Frustum2");
    }

    private bool IsObjectGrabbed(GameObject obj)
    {
        if (!ignoreGrabbedObjects) return false;
        
        var grabInteractable = obj.GetComponentInParent<GrabInteractable>();
        if (grabInteractable != null && grabInteractable.State == InteractableState.Select)
        {
            return true;
        }
        
        
        return false;
    }

    private void CheckFrustumObjects(System.Collections.Generic.List<GameObject> objectsInFrustum, string frustumName)
    {
        foreach (var obj in objectsInFrustum)
        {
            if (obj == null) continue;
            
            if (IsObjectGrabbed(obj))
            {
                if (enableDebugLogs && objectLastPositions.ContainsKey(obj))
                {
                    Debug.Log($"[FearMeter] [{frustumName}] Object '{obj.name}' is grabbed - ignoring movement");
                }
                
                objectLastPositions[obj] = obj.transform.position;
                continue;
            }
            
            if (objectLastPositions.ContainsKey(obj))
            {
                Vector3 lastPos = objectLastPositions[obj];
                Vector3 currentPos = obj.transform.position;
                
                float distance = Vector3.Distance(lastPos, currentPos);
                
                if (distance > movementThreshold)
                {
                    float fearAdded = fearPerDetection * Time.deltaTime;
                    AddFear(fearAdded);
                    objectLastPositions[obj] = currentPos;
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[FearMeter] [{frustumName}] Object '{obj.name}' moved {distance:F3}m (not grabbed). Adding {fearAdded:F2} fear. Total fear: {currentFear.Value:F2}/{maxFear}");
                    }
                }
            }
            else
            {
                objectLastPositions[obj] = obj.transform.position;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[FearMeter] [{frustumName}] Now tracking object '{obj.name}' at position {obj.transform.position}");
                }
            }
        }
        
        var allObjects = frustumDetector.GetObjectsInAnyFrustum();
        var keysToRemove = new System.Collections.Generic.List<GameObject>();
        
        foreach (var key in objectLastPositions.Keys)
        {
            if (key == null || !allObjects.Contains(key))
            {
                keysToRemove.Add(key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            if (enableDebugLogs && key != null)
            {
                Debug.Log($"[FearMeter] Stopped tracking object '{key.name}' (no longer in any frustum)");
            }
            objectLastPositions.Remove(key);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddFearServerRpc(float amount)
    {
        AddFear(amount);
    }

    private void AddFear(float amount)
    {
        if (!IsServer) return;
        
        float oldFear = currentFear.Value;
        currentFear.Value = Mathf.Clamp(currentFear.Value + amount, 0f, maxFear);

        if (!wasFull && currentFear.Value >= maxFear)
        {
            OnFearFull();
        }
        
        if (enableDebugLogs && Mathf.Abs(currentFear.Value - oldFear) > 0.001f)
        {
            Debug.Log($"[FearMeter] Fear increased: {oldFear:F2} -> {currentFear.Value:F2} (+{amount:F2})");
        }
    }

    private void OnFearFull()
    {
        GameManager.Instance.SetClientWinCondition(true);
        Debug.Log("[FearMeter] Fear meter is FULL!");
    }


    public void ResetFear()
    {
        if (!IsServer) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[FearMeter] Resetting fear from {currentFear.Value:F2} to 0");
        }
        
        currentFear.Value = 0f;
    }

    private void OnFearValueChanged(float oldValue, float newValue)
    {
        UpdateMeterVisual(newValue);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[FearMeter] Fear value synced: {oldValue:F2} -> {newValue:F2} (Percentage: {(newValue/maxFear)*100:F1}%)");
        }
    }

    private void UpdateMeterVisual(float fearValue)
    {
        if (fillCylinder == null) return;
        
        float fillPercentage = Mathf.Clamp01(fearValue / maxFear);

        float targetHeight = meterHeight * fillPercentage;
        fillCylinder.transform.localScale = new Vector3(
            fillBaseScale.x,
            targetHeight / 2,
            fillBaseScale.z
        );
        
        fillCylinder.transform.localPosition = new Vector3(
            0,
            -meterHeight / 2 + targetHeight / 2,
            0
        );
        
        if (fillMaterial != null)
        {
            Color intensifiedColor = Color.Lerp(fillColor * 0.5f, fillColor, fillPercentage);
            fillMaterial.SetColor("_EmissionColor", intensifiedColor * fillPercentage);
        }
    }

    public float GetCurrentFear()
    {
        return currentFear.Value;
    }

    public float GetFearPercentage()
    {
        return currentFear.Value / maxFear;
    }

    private void OnDestroy()
    {
        if (meterContainer != null)
        {
            Destroy(meterContainer);
        }
    }
}