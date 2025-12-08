using UnityEngine;
using Oculus.Interaction;
using System.Linq;

/// <summary>
/// Visualizes a preview of the object that will snap when hovering over a snap interactable
/// </summary>
public class SnapInteractableHoverVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    [Tooltip("The SnapInteractable component to monitor")]
    private SnapInteractable snapInteractable;

    [SerializeField]
    [Tooltip("The material to apply to the preview (should be transparent)")]
    private Material hoverMaterial;

    [Header("Visualization Settings")]
    [SerializeField]
    [Tooltip("Show preview at the snap point location")]
    private bool showPreviewAtSnapPoint = true;

    [SerializeField]
    [Tooltip("Include child meshes in the preview")]
    private bool includeChildMeshes = true;

    private GameObject previewObject;
    private bool isHovering = false;
    private IInteractorView currentInteractor;

    private void Awake()
    {
        // Auto-find SnapInteractable if not assigned
        if (snapInteractable == null)
        {
            snapInteractable = GetComponent<SnapInteractable>();
        }

        if (snapInteractable == null)
        {
            Debug.LogError("SnapInteractable not found! Please assign it in the inspector.", this);
            enabled = false;
            return;
        }

        if (hoverMaterial == null)
        {
            Debug.LogWarning("Hover material not assigned! Visualization will not work.", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (snapInteractable != null)
        {
            snapInteractable.WhenInteractorViewAdded += HandleHoverStart;
            snapInteractable.WhenInteractorViewRemoved += HandleHoverEnd;
        }
    }

    private void OnDisable()
    {
        if (snapInteractable != null)
        {
            snapInteractable.WhenInteractorViewAdded -= HandleHoverStart;
            snapInteractable.WhenInteractorViewRemoved -= HandleHoverEnd;
        }

        // Clean up visualization
        HideVisualization();
    }

    private void HandleHoverStart(IInteractorView interactorView)
    {
        if (!isHovering)
        {
            isHovering = true;
            currentInteractor = interactorView;
            ShowVisualization();
        }
    }

    private void HandleHoverEnd(IInteractorView interactorView)
    {
        // Check if there are still other interactors hovering
        if (!snapInteractable.InteractorViews.Any())
        {
            isHovering = false;
            currentInteractor = null;
            HideVisualization();
        }
    }

    private void ShowVisualization()
    {
        // Get the object that's being held/will snap
        GameObject snappableObject = GetSnappableObject();
        
        if (snappableObject == null)
        {
            return;
        }

        // Create preview from the snappable object
        CreatePreviewFromObject(snappableObject);
    }

    private GameObject GetSnappableObject()
    {
        if (currentInteractor == null)
            return null;

        // Try to get the interactor's data (the thing being grabbed)
        if (currentInteractor.Data is IInteractable interactable)
        {
            // Get the GameObject from the interactable
            if (interactable is MonoBehaviour mono)
            {
                return mono.gameObject;
            }
        }

        // Alternative: Try to get from the interactor itself
        if (currentInteractor is MonoBehaviour interactorMono)
        {
            // Check if the interactor has a Grabbable or similar component
            var grabbable = interactorMono.GetComponentInParent<Grabbable>();
            if (grabbable != null)
            {
                return grabbable.gameObject;
            }

            // Fallback: use the interactor's gameObject itself
            return interactorMono.gameObject;
        }

        return null;
    }

    private void CreatePreviewFromObject(GameObject sourceObject)
    {
        // Clean up any existing preview
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        // Create preview container
        previewObject = new GameObject("SnapPreview");
        
        if (showPreviewAtSnapPoint)
        {
            previewObject.transform.position = snapInteractable.transform.position;
            previewObject.transform.rotation = snapInteractable.transform.rotation;
        }
        else
        {
            previewObject.transform.position = sourceObject.transform.position;
            previewObject.transform.rotation = sourceObject.transform.rotation;
        }

        // Copy all meshes from source object
        if (includeChildMeshes)
        {
            CopyMeshHierarchy(sourceObject, previewObject);
        }
        else
        {
            CopyMeshFromObject(sourceObject, previewObject);
        }
    }

    private void CopyMeshHierarchy(GameObject source, GameObject destination)
    {
        // Get all MeshFilters in the source object and its children
        MeshFilter[] sourceMeshFilters = source.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter sourceMeshFilter in sourceMeshFilters)
        {
            if (sourceMeshFilter.sharedMesh == null)
                continue;

            // Create a corresponding GameObject in the preview hierarchy
            GameObject previewChild = new GameObject(sourceMeshFilter.gameObject.name + "_Preview");
            previewChild.transform.SetParent(destination.transform, false);

            // Calculate local transform relative to source root
            Transform sourceTransform = sourceMeshFilter.transform;
            Transform sourceRoot = source.transform;

            previewChild.transform.localPosition = sourceRoot.InverseTransformPoint(sourceTransform.position);
            previewChild.transform.localRotation = Quaternion.Inverse(sourceRoot.rotation) * sourceTransform.rotation;
            previewChild.transform.localScale = sourceTransform.localScale;

            // Add mesh components
            MeshFilter previewMeshFilter = previewChild.AddComponent<MeshFilter>();
            previewMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            MeshRenderer previewRenderer = previewChild.AddComponent<MeshRenderer>();
            
            // Apply hover material to all materials
            Material[] materials = new Material[sourceMeshFilter.GetComponent<MeshRenderer>().sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = hoverMaterial;
            }
            previewRenderer.sharedMaterials = materials;
        }
    }

    private void CopyMeshFromObject(GameObject source, GameObject destination)
    {
        MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();
        
        if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"No MeshFilter found on {source.name}", this);
            return;
        }

        // Add mesh components to preview
        MeshFilter previewMeshFilter = destination.AddComponent<MeshFilter>();
        previewMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

        MeshRenderer previewRenderer = destination.AddComponent<MeshRenderer>();
        
        // Apply hover material
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceRenderer != null)
        {
            Material[] materials = new Material[sourceRenderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = hoverMaterial;
            }
            previewRenderer.sharedMaterials = materials;
        }
        else
        {
            previewRenderer.sharedMaterial = hoverMaterial;
        }
    }

    private void HideVisualization()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    private void Update()
    {
        // Update preview position if it exists and we're showing at snap point
        if (isHovering && previewObject != null && showPreviewAtSnapPoint)
        {
            previewObject.transform.position = snapInteractable.transform.position;
            previewObject.transform.rotation = snapInteractable.transform.rotation;
        }
    }

    private void OnDestroy()
    {
        HideVisualization();
    }
}