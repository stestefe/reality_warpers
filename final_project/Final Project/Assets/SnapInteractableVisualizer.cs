using UnityEngine;
using Oculus.Interaction;
using System.Linq;

/// <summary>
/// Visualizes a preview of the object that will snap when hovering over a snap interactable
/// </summary>
public class SnapInteractableHoverVisualizer : MonoBehaviour
{
    [SerializeField]
    private SnapInteractable snapInteractable;

    [SerializeField]
    private Material hoverMaterial;

    [SerializeField]
    private bool showPreviewAtSnapPoint = true;

    [SerializeField]
    private bool includeChildMeshes = true;

    private GameObject previewObject;
    private bool isHovering = false;
    private IInteractorView currentInteractor;

    private void Awake()
    {
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
            snapInteractable.WhenSelectingInteractorViewAdded += HandleSnapped;
        }
    }

    private void OnDisable()
    {
        if (snapInteractable != null)
        {
            snapInteractable.WhenInteractorViewAdded -= HandleHoverStart;
            snapInteractable.WhenInteractorViewRemoved -= HandleHoverEnd;
            snapInteractable.WhenSelectingInteractorViewAdded -= HandleSnapped;
        }

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
        if (!snapInteractable.InteractorViews.Any())
        {
            isHovering = false;
            currentInteractor = null;
            HideVisualization();
        }
    }

    private void HandleSnapped(IInteractorView interactorView)
    {
        isHovering = false;
        currentInteractor = null;
        HideVisualization();
    }

    private void ShowVisualization()
    {
        GameObject snappableObject = GetSnappableObject();
        
        if (snappableObject == null)
        {
            return;
        }

        CreatePreviewFromObject(snappableObject);
    }

    private GameObject GetSnappableObject()
    {
        if (currentInteractor == null)
            return null;

        if (currentInteractor.Data is IInteractable interactable)
        {
            if (interactable is MonoBehaviour mono)
            {
                return mono.gameObject;
            }
        }

        if (currentInteractor is MonoBehaviour interactorMono)
        {
            var grabbable = interactorMono.GetComponentInParent<Grabbable>();
            if (grabbable != null)
            {
                return grabbable.gameObject;
            }

            return interactorMono.gameObject;
        }

        return null;
    }

    private void CreatePreviewFromObject(GameObject sourceObject)
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

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
        MeshFilter[] sourceMeshFilters = source.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter sourceMeshFilter in sourceMeshFilters)
        {
            if (sourceMeshFilter.sharedMesh == null)
                continue;

            GameObject previewChild = new GameObject(sourceMeshFilter.gameObject.name + "_Preview");
            previewChild.transform.SetParent(destination.transform, false);

            Transform sourceTransform = sourceMeshFilter.transform;
            Transform sourceRoot = source.transform;

            previewChild.transform.localPosition = sourceRoot.InverseTransformPoint(sourceTransform.position);
            previewChild.transform.localRotation = Quaternion.Inverse(sourceRoot.rotation) * sourceTransform.rotation;
            previewChild.transform.localScale = sourceTransform.localScale;

            MeshFilter previewMeshFilter = previewChild.AddComponent<MeshFilter>();
            previewMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            MeshRenderer previewRenderer = previewChild.AddComponent<MeshRenderer>();
            
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

        MeshFilter previewMeshFilter = destination.AddComponent<MeshFilter>();
        previewMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

        MeshRenderer previewRenderer = destination.AddComponent<MeshRenderer>();
        
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