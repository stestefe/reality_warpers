using UnityEngine;
using Unity.Netcode;

public class VirtualVRCooldownVisualizer : MonoBehaviour
{
    [Header("References")]
    public Transform controllerTransform;
    public ControllerBeam beamScript;
    
    [Header("Radial Display")]
    public float radius = 0.05f;
    public float distance = 0.1f;
    public int segments = 32;
    public Color cooldownColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    public Color readyColor = new Color(0.3f, 1f, 0.3f, 0.8f);
    
    [Header("Pulse Effect")]
    public bool enablePulse = true;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.2f;

    private GameObject radialIndicator;
    private MeshRenderer indicatorRenderer;
    private Material indicatorMaterial;
    private Mesh radialMesh;

    void Start()
    {
        if (controllerTransform == null)
        {
            controllerTransform = transform;
        }

        if (beamScript == null)
        {
            beamScript = GetComponent<ControllerBeam>();
            if (beamScript == null)
            {
                Debug.LogError("VRCooldownVisualizer: ControllerBeam not found! Please assign it in the inspector.");
                enabled = false;
                return;
            }
        }

        CreateRadialIndicator();
    }

    void CreateRadialIndicator()
    {
        radialIndicator = new GameObject("CooldownRadialIndicator");
        radialIndicator.transform.SetParent(controllerTransform, false);
        radialIndicator.transform.localPosition = new Vector3(0, 0, distance);
        radialIndicator.transform.localRotation = Quaternion.Euler(-90, 0, 0);

        MeshFilter meshFilter = radialIndicator.AddComponent<MeshFilter>();
        indicatorRenderer = radialIndicator.AddComponent<MeshRenderer>();

        indicatorMaterial = new Material(Shader.Find("Unlit/Color"));
        indicatorMaterial.color = readyColor;
        indicatorRenderer.material = indicatorMaterial;

        radialMesh = new Mesh();
        radialMesh.name = "CooldownRadialMesh";
        meshFilter.mesh = radialMesh;
        
        UpdateRadialMesh(1f);
        
        radialIndicator.SetActive(false);
    }

    void Update()
{
    if (beamScript == null || radialIndicator == null) return;

    float cooldownProgress = beamScript.GetCooldownProgress();
    
    if (!radialIndicator.activeSelf)
    {
        radialIndicator.SetActive(true);
    }

    if (beamScript.IsOnCooldown)
    {
        UpdateRadialMesh(1f - cooldownProgress);

        if (indicatorMaterial != null)
        {
            indicatorMaterial.color = cooldownColor;
        }
        
        // Reset scale during cooldown
        radialIndicator.transform.localScale = Vector3.one;
    }
    else
    {
        UpdateRadialMesh(1f);
        
        if (indicatorMaterial != null)
        {
            indicatorMaterial.color = readyColor;
        }

        // Apply pulse effect when ready
        if (enablePulse)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            radialIndicator.transform.localScale = Vector3.one * pulse;
        }
        else
        {
            radialIndicator.transform.localScale = Vector3.one;
        }
    }
}

    void UpdateRadialMesh(float fillAmount)
    {
        fillAmount = Mathf.Clamp01(fillAmount);
        
        int activeSegments = Mathf.CeilToInt(segments * fillAmount);
        int vertexCount = activeSegments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[activeSegments * 3];

        vertices[0] = Vector3.zero;

        float angleStep = (Mathf.PI * 2f) / segments;
        float currentAngle = -Mathf.PI / 2f;

        for (int i = 0; i <= activeSegments; i++)
        {
            float angle = currentAngle + (angleStep * i);
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            vertices[i + 1] = new Vector3(x, y, 0);
        }

        for (int i = 0; i < activeSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        radialMesh.Clear();
        radialMesh.vertices = vertices;
        radialMesh.triangles = triangles;
        radialMesh.RecalculateNormals();
    }

    void OnDestroy()
    {
        if (indicatorMaterial != null)
        {
            Destroy(indicatorMaterial);
        }
        if (radialMesh != null)
        {
            Destroy(radialMesh);
        }
        if (radialIndicator != null)
        {
            Destroy(radialIndicator);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (controllerTransform == null) return;

        Gizmos.color = Color.yellow;
        Vector3 indicatorPos = controllerTransform.position + controllerTransform.forward * distance;
        Gizmos.DrawWireSphere(indicatorPos, radius);
    }
}