using UnityEngine;
using Oculus.Interaction;

public class SnapTagFilter : MonoBehaviour, IGameObjectFilter
{
    [Header("Snap Requirements")]
    [SerializeField]
    private string requiredTag = "CubeA";

    [Header("Gaze Requirements")]
    [SerializeField]
    private GameObject requiredGazeObject;
    
    [SerializeField]
    private float requiredGazeDuration = 3f;
    
    [SerializeField]
    private DualTrapezoPrismWireframe frustumWireframe;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private SnapInteractable snapInteractable;
    private bool hasLookedAtRequiredObject = false;
    private float gazeTimer = 0f;
    private bool isCurrentlyLookingAtRequiredObject = false;

    private void Awake()
    {
        snapInteractable = GetComponent<SnapInteractable>();
        
        if (snapInteractable == null)
        {
            Debug.LogError("SnapTagFilter requires a SnapInteractable component!", this);
            enabled = false;
            return;
        }

        if (frustumWireframe == null)
        {
            frustumWireframe = FindObjectOfType<DualTrapezoPrismWireframe>();
            if (frustumWireframe == null)
            {
                Debug.LogError("SnapTagFilter: DualTrapezoPrismWireframe not found in scene!", this);
                enabled = false;
                return;
            }
        }

        if (requiredGazeObject == null)
        {
            Debug.LogWarning("SnapTagFilter: No required gaze object assigned. Gaze requirement will be disabled.", this);
        }
    }

    private void Update()
    {
        if (!hasLookedAtRequiredObject && requiredGazeObject != null && frustumWireframe != null)
        {
            TrackGaze();
        }
    }

    private void TrackGaze()
    {
        bool isInFrustum = frustumWireframe.IsObjectInFrustum2(requiredGazeObject);

        if (isInFrustum)
        {
            if (!isCurrentlyLookingAtRequiredObject)
            {
                isCurrentlyLookingAtRequiredObject = true;
                if (showDebugLogs)
                {
                    Debug.Log($"Started looking at {requiredGazeObject.name}");
                }
            }

            gazeTimer += Time.deltaTime;

            if (showDebugLogs && gazeTimer % 1f < Time.deltaTime)
            {
                Debug.Log($"Gaze timer: {gazeTimer:F1}s / {requiredGazeDuration}s");
            }

            if (gazeTimer >= requiredGazeDuration && !hasLookedAtRequiredObject)
            {
                hasLookedAtRequiredObject = true;
                if (showDebugLogs)
                {
                    Debug.Log($"<color=green>Gaze requirement met! {requiredGazeObject.name} has been looked at for {requiredGazeDuration}s. Snapping is now enabled.</color>");
                }
            }
        }
        else
        {
            if (isCurrentlyLookingAtRequiredObject)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"Stopped looking at {requiredGazeObject.name}. Timer reset from {gazeTimer:F1}s");
                }
                isCurrentlyLookingAtRequiredObject = false;
                gazeTimer = 0f;
            }
        }
    }

    public bool Filter(GameObject go)
    {
        bool hasTag = go.CompareTag(requiredTag);
        
        if (!hasTag)
        {
            if (showDebugLogs)
            {
                Debug.Log($"Object {go.name} cannot snap - requires tag '{requiredTag}' but has '{go.tag}'");
            }
            return false;
        }

        if (requiredGazeObject != null && !hasLookedAtRequiredObject)
        {
            if (showDebugLogs)
            {
                Debug.Log($"Object {go.name} cannot snap yet - must look at {requiredGazeObject.name} for {requiredGazeDuration}s first (current: {gazeTimer:F1}s)");
            }
            return false;
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>Object {go.name} can snap - all requirements met!</color>");
        }
        return true;
    }

    public bool HasMetGazeRequirement()
    {
        return hasLookedAtRequiredObject;
    }

    public float GetCurrentGazeTime()
    {
        return gazeTimer;
    }

    public void ResetGazeRequirement()
    {
        hasLookedAtRequiredObject = false;
        gazeTimer = 0f;
        isCurrentlyLookingAtRequiredObject = false;
        if (showDebugLogs)
        {
            Debug.Log("Gaze requirement reset");
        }
    }

    private void OnGUI()
    {
        if (showDebugLogs && requiredGazeObject != null && !hasLookedAtRequiredObject)
        {
            GUI.Label(new Rect(10, 10, 400, 30), 
                $"Gaze Progress: {gazeTimer:F1}s / {requiredGazeDuration}s");
            
            if (isCurrentlyLookingAtRequiredObject)
            {
                GUI.Label(new Rect(10, 40, 400, 30), 
                    $"Looking at: {requiredGazeObject.name}");
            }
        }
    }
}