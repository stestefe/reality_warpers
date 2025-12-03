using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Transform))]
public class DynamicDualTrapezoPrismWireframe : MonoBehaviour
{
    [System.Serializable]
    public class FrustumConfig
    {
        [Header("dimensions")]
        public float nearHalfWidth = 0.05f;
        public float nearHalfHeight = 0.05f;
        public float farHalfWidth = 0.3f;
        public float farHalfHeight = 0.2f;
        public float depth = 0.5f;

        [Header("appearance")]
        public float lineWidth = 0.005f;
        public Material lineMaterial;
    }

    [Header("frustum 1")]
    public FrustumConfig frustum1 = new FrustumConfig
    {
        nearHalfWidth = 0.27f,
        nearHalfHeight = 1.5f,
        farHalfWidth = 2.8f,
        farHalfHeight = 1.5f,
        depth = 3.1f,
        lineWidth = 0.005f
    };

    [Header("frustum 2")]
    public FrustumConfig frustum2 = new FrustumConfig
    {
        nearHalfWidth = 0.27f,
        nearHalfHeight = 1.5f,
        farHalfWidth = 0.99f,
        farHalfHeight = 1.5f,
        depth = 3.1f,
        lineWidth = 0.005f
    };

    [Header("Dynamic Target Settings")]
    public bool parentToDynamicTarget = true;
    public string targetObjectName = "CenterEyeAnchor";
    public float searchInterval = 0.5f;

    [Header("settings")]
    public bool visibleInEditor = true;

    [Header("collision detect")]
    public bool enableCollisionDetection = true;
    public string targetTag = "Target";

    private FrustumInstance instance1;
    private FrustumInstance instance2;
    private Transform wireParent;
    private Transform targetTransform;
    private GameObject containerObject;
    private bool targetFound = false;
    private Coroutine searchCoroutine;
    
    private List<GameObject> objectsInFrustum1 = new List<GameObject>();
    private List<GameObject> objectsInFrustum2 = new List<GameObject>();
    private List<GameObject> objectsInAnyFrustum = new List<GameObject>();

    private static readonly int[,] edges = new int[,]
    {
        {0,1},{1,2},{2,3},{3,0},
        {4,5},{5,6},{6,7},{7,4},
        {0,4},{1,5},{2,6},{3,7}
    };

    private class FrustumInstance
    {
        public List<LineRenderer> edgeRenderers = new List<LineRenderer>();
        public GameObject edgesContainerObject;
        public GameObject colliderObject;
        public BoxCollider frustumCollider;
        public Vector3[] vertices = new Vector3[8];
        public FrustumConfig config;
        public string name;
    }

    private void OnEnable() 
    { 
        CleanupAllOrphanedContainers();
        
        if (Application.isPlaying && parentToDynamicTarget)
        {
            targetFound = false;
            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
            }
            searchCoroutine = StartCoroutine(SearchForTargetObject());
        }
        else
        {
            FindTargetTransform();
            EnsureParentIfNeeded();
            Rebuild();
        }
    }
    
    private void OnDisable() 
    { 
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
        
        DestroyAllWireframesImmediate(); 
        CleanupContainers();
    }

    private void OnDestroy()
    {
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
        
        DestroyAllWireframesImmediate();
        CleanupContainers();
    }

    private void OnValidate() 
    { 
        if (!Application.isPlaying)
        {
            CleanupAllOrphanedContainers();
        }
        Rebuild(); 
    }

    private void Update() 
    { 
        if (!Application.isPlaying) 
        {
            SetWireVisibility(visibleInEditor);
        }
        else if (parentToDynamicTarget && targetFound && targetTransform == null)
        {
            Debug.LogWarning($"arget object '{targetObjectName}' was destroyed");
            targetFound = false;
            if (searchCoroutine == null)
            {
                searchCoroutine = StartCoroutine(SearchForTargetObject());
            }
        }
    }

    private IEnumerator SearchForTargetObject()
    {
        Debug.Log($"earching for target object '{targetObjectName}'...");
        
        while (!targetFound)
        {
            GameObject targetObject = GameObject.Find(targetObjectName);
            
            if (targetObject != null)
            {
                targetTransform = targetObject.transform;
                targetFound = true;
                Debug.Log($"ound target object '{targetObjectName}'.");
                
                EnsureParentIfNeeded();
                Rebuild();
                
                searchCoroutine = null;
                yield break;
            }
            
            yield return new WaitForSeconds(searchInterval);
        }
    }

    private void FindTargetTransform()
    {
        if (!parentToDynamicTarget) return;
        
        GameObject targetObject = GameObject.Find(targetObjectName);
        if (targetObject != null)
        {
            targetTransform = targetObject.transform;
            targetFound = true;
            Debug.Log($"ound target object '{targetObjectName}'");
        }
        else
        {
            Debug.LogWarning($"ould not find object named '{targetObjectName}'");
        }
    }

    private void EnsureParentIfNeeded()
    {
        if (containerObject != null && wireParent != null)
        {
            return;
        }

        CleanupContainers();

        if (!parentToDynamicTarget) 
        { 
            wireParent = transform; 
            return; 
        }

        if (targetTransform != null)
        {
            containerObject = new GameObject("DualFrustumWireframe_Container");
            containerObject.transform.SetParent(targetTransform, false);
            containerObject.transform.localPosition = Vector3.zero;
            containerObject.transform.localRotation = Quaternion.identity;
            containerObject.transform.localScale = Vector3.one;
            wireParent = containerObject.transform;
            
#if UNITY_EDITOR
            containerObject.hideFlags = HideFlags.DontSaveInEditor;
#endif
        }
        else 
        {
            wireParent = transform;
        }
    }

    private void Rebuild()
    {
        DestroyAllWireframesImmediate();
        
        if (wireParent == null)
        {
            EnsureParentIfNeeded();
        }

        if (wireParent == null) return;

        instance1 = BuildFrustumInstance(frustum1, "Frustum1");
        instance2 = BuildFrustumInstance(frustum2, "Frustum2");
    }

    private FrustumInstance BuildFrustumInstance(FrustumConfig config, string name)
    {
        FrustumInstance instance = new FrustumInstance
        {
            config = config,
            name = name
        };

        float cameraWorldY = wireParent.position.y;

        instance.vertices[0] = new Vector3(-config.nearHalfWidth, -cameraWorldY, 0f);
        instance.vertices[1] = new Vector3(config.nearHalfWidth, -cameraWorldY, 0f);
        instance.vertices[2] = new Vector3(config.nearHalfWidth, config.nearHalfHeight * 2f - cameraWorldY, 0f);
        instance.vertices[3] = new Vector3(-config.nearHalfWidth, config.nearHalfHeight * 2f - cameraWorldY, 0f);

        instance.vertices[4] = new Vector3(-config.farHalfWidth, -cameraWorldY, config.depth);
        instance.vertices[5] = new Vector3(config.farHalfWidth, -cameraWorldY, config.depth);
        instance.vertices[6] = new Vector3(config.farHalfWidth, config.farHalfHeight * 2f - cameraWorldY, config.depth);
        instance.vertices[7] = new Vector3(-config.farHalfWidth, config.farHalfHeight * 2f - cameraWorldY, config.depth);

        GameObject edgesContainer = new GameObject($"{name}_Edges");
        edgesContainer.transform.SetParent(wireParent, false);
        edgesContainer.transform.localPosition = Vector3.zero;
        edgesContainer.transform.localRotation = Quaternion.identity;
        edgesContainer.transform.localScale = Vector3.one;
        instance.edgesContainerObject = edgesContainer;

#if UNITY_EDITOR
        edgesContainer.hideFlags = HideFlags.DontSaveInEditor;
#endif

        for (int e = 0; e < edges.GetLength(0); e++)
        {
            int i0 = edges[e,0]; 
            int i1 = edges[e,1];
            
            GameObject lineGO = new GameObject($"{name}_edge_{i0}_{i1}");
            lineGO.transform.SetParent(edgesContainer.transform, false);
            
            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, instance.vertices[i0]);
            lr.SetPosition(1, instance.vertices[i1]);
            lr.useWorldSpace = false;
            lr.widthCurve = AnimationCurve.Constant(0f, 1f, config.lineWidth);
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            
            if (config.lineMaterial != null) 
            {
                lr.material = config.lineMaterial;
            }
            else 
            { 
                Shader shader = Shader.Find("Unlit/Color"); 
                if (shader != null)
                { 
                    Material m = new Material(shader); 
                    m.SetColor("_Color", name == "Frustum1" ? Color.green : Color.red); 
                    lr.material = m; 
                } 
            }
            
            instance.edgeRenderers.Add(lr);
        }

        if (enableCollisionDetection && Application.isPlaying)
        {
            CreateFrustumCollider(instance);
        }

        return instance;
    }

    private void CreateFrustumCollider(FrustumInstance instance)
    {
        if (instance.colliderObject != null)
        {
            if (Application.isPlaying)
                Destroy(instance.colliderObject);
            else
                DestroyImmediate(instance.colliderObject);
        }

        instance.colliderObject = new GameObject($"{instance.name}_Collider");
        instance.colliderObject.transform.SetParent(wireParent, false);
        instance.colliderObject.transform.localPosition = new Vector3(0, 0, instance.config.depth / 2f);
        instance.colliderObject.transform.localRotation = Quaternion.identity;
        instance.colliderObject.transform.localScale = Vector3.one;

        Rigidbody rb = instance.colliderObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        instance.frustumCollider = instance.colliderObject.AddComponent<BoxCollider>();
        instance.frustumCollider.isTrigger = true;
        instance.frustumCollider.size = new Vector3(instance.config.farHalfWidth * 2f, instance.config.farHalfHeight * 2f, instance.config.depth);

        DynamicFrustumTriggerHandler handler = instance.colliderObject.AddComponent<DynamicFrustumTriggerHandler>();
        handler.Initialize(this, targetTag, instance.name);

#if UNITY_EDITOR
        instance.colliderObject.hideFlags = HideFlags.DontSaveInEditor;
#endif
    }

    public void OnObjectEnteredFrustum(GameObject obj, string frustumName)
    {
        if (frustumName == "Frustum1")
        {
            if (!objectsInFrustum1.Contains(obj))
            {
                objectsInFrustum1.Add(obj);
                Debug.Log($"[{frustumName}] Object entered: {obj.name}");
            }
        }
        else if (frustumName == "Frustum2")
        {
            if (!objectsInFrustum2.Contains(obj))
            {
                objectsInFrustum2.Add(obj);
                Debug.Log($"[{frustumName}] Object entered: {obj.name}");
            }
        }

        if (!objectsInAnyFrustum.Contains(obj))
        {
            objectsInAnyFrustum.Add(obj);
        }
    }

    public void OnObjectExitedFrustum(GameObject obj, string frustumName)
    {
        if (frustumName == "Frustum1")
        {
            if (objectsInFrustum1.Contains(obj))
            {
                objectsInFrustum1.Remove(obj);
                Debug.Log($"[{frustumName}] Object exited: {obj.name}");
            }
        }
        else if (frustumName == "Frustum2")
        {
            if (objectsInFrustum2.Contains(obj))
            {
                objectsInFrustum2.Remove(obj);
                Debug.Log($"[{frustumName}] Object exited: {obj.name}");
            }
        }

        if (!objectsInFrustum1.Contains(obj) && !objectsInFrustum2.Contains(obj))
        {
            objectsInAnyFrustum.Remove(obj);
        }
    }

    public List<GameObject> GetObjectsInFrustum1()
    {
        return new List<GameObject>(objectsInFrustum1);
    }

    public List<GameObject> GetObjectsInFrustum2()
    {
        return new List<GameObject>(objectsInFrustum2);
    }

    public List<GameObject> GetObjectsInAnyFrustum()
    {
        return new List<GameObject>(objectsInAnyFrustum);
    }

    public int GetObjectCountInFrustum1()
    {
        return objectsInFrustum1.Count;
    }

    public int GetObjectCountInFrustum2()
    {
        return objectsInFrustum2.Count;
    }

    public int GetTotalObjectCount()
    {
        return objectsInAnyFrustum.Count;
    }

    public bool IsObjectInFrustum1(GameObject obj)
    {
        return objectsInFrustum1.Contains(obj);
    }

    public bool IsObjectInFrustum2(GameObject obj)
    {
        return objectsInFrustum2.Contains(obj);
    }

    public bool IsObjectInAnyFrustum(GameObject obj)
    {
        return objectsInAnyFrustum.Contains(obj);
    }

    public bool IsTargetFound()
    {
        return targetFound && targetTransform != null;
    }

    public void ForceSearchForTarget()
    {
        if (Application.isPlaying && parentToDynamicTarget)
        {
            targetFound = false;
            if (searchCoroutine != null)
            {
                StopCoroutine(searchCoroutine);
            }
            searchCoroutine = StartCoroutine(SearchForTargetObject());
        }
    }

    private void LateUpdate()
    {
        if (wireParent == null || !parentToDynamicTarget) return;

        if (targetTransform == null)
        {
            return;
        }

        wireParent.position = targetTransform.position;

        Vector3 euler = targetTransform.rotation.eulerAngles;
        wireParent.rotation = Quaternion.Euler(0f, euler.y, 0f);

        if (instance1 != null) UpdateVerticesForGroundAlignment(instance1);
        if (instance2 != null) UpdateVerticesForGroundAlignment(instance2);
    }

    private void UpdateVerticesForGroundAlignment(FrustumInstance instance)
    {
        if (wireParent == null || instance.edgeRenderers.Count == 0) return;

        float cameraWorldY = wireParent.position.y;
        FrustumConfig config = instance.config;

        instance.vertices[0] = new Vector3(-config.nearHalfWidth, -cameraWorldY, 0f);
        instance.vertices[1] = new Vector3(config.nearHalfWidth, -cameraWorldY, 0f);
        instance.vertices[2] = new Vector3(config.nearHalfWidth, config.nearHalfHeight * 2f - cameraWorldY, 0f);
        instance.vertices[3] = new Vector3(-config.nearHalfWidth, config.nearHalfHeight * 2f - cameraWorldY, 0f);

        instance.vertices[4] = new Vector3(-config.farHalfWidth, -cameraWorldY, config.depth);
        instance.vertices[5] = new Vector3(config.farHalfWidth, -cameraWorldY, config.depth);
        instance.vertices[6] = new Vector3(config.farHalfWidth, config.farHalfHeight * 2f - cameraWorldY, config.depth);
        instance.vertices[7] = new Vector3(-config.farHalfWidth, config.farHalfHeight * 2f - cameraWorldY, config.depth);

        int edgeIndex = 0;
        for (int e = 0; e < edges.GetLength(0); e++)
        {
            if (edgeIndex >= instance.edgeRenderers.Count) break;

            LineRenderer lr = instance.edgeRenderers[edgeIndex];
            if (lr != null)
            {
                int i0 = edges[e, 0];
                int i1 = edges[e, 1];
                lr.SetPosition(0, instance.vertices[i0]);
                lr.SetPosition(1, instance.vertices[i1]);
            }
            edgeIndex++;
        }
    }

    private void DestroyFrustumInstance(FrustumInstance instance)
    {
        if (instance == null) return;

        foreach (var lr in instance.edgeRenderers) 
        { 
            if (lr == null) continue; 
            if (Application.isPlaying) 
                Destroy(lr.gameObject); 
            else 
                DestroyImmediate(lr.gameObject); 
        }
        instance.edgeRenderers.Clear();
        
        if (instance.edgesContainerObject != null)
        {
            if (Application.isPlaying)
                Destroy(instance.edgesContainerObject);
            else
                DestroyImmediate(instance.edgesContainerObject);
        }

        if (instance.colliderObject != null)
        {
            if (Application.isPlaying)
                Destroy(instance.colliderObject);
            else
                DestroyImmediate(instance.colliderObject);
        }
    }

    private void DestroyAllWireframesImmediate()
    {
        DestroyFrustumInstance(instance1);
        DestroyFrustumInstance(instance2);
        instance1 = null;
        instance2 = null;
        objectsInFrustum1.Clear();
        objectsInFrustum2.Clear();
        objectsInAnyFrustum.Clear();
    }

    private void CleanupContainers()
    {
        if (containerObject != null)
        {
            if (Application.isPlaying)
                Destroy(containerObject);
            else
                DestroyImmediate(containerObject);
            containerObject = null;
        }
        
        wireParent = null;
    }

    private void CleanupAllOrphanedContainers()
    {
#if UNITY_EDITOR
        if (targetTransform != null)
        {
            Transform[] children = targetTransform.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == "DualFrustumWireframe_Container" && child != containerObject?.transform)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if ((obj.name.Contains("Frustum1_") || obj.name.Contains("Frustum2_")) &&
                (instance1 == null || (obj != instance1.edgesContainerObject && obj != instance1.colliderObject)) &&
                (instance2 == null || (obj != instance2.edgesContainerObject && obj != instance2.colliderObject)))
            {
                DestroyImmediate(obj);
            }
        }
#endif
    }

    private void SetWireVisibility(bool visible)
    { 
        if (instance1 != null)
        {
            foreach (var lr in instance1.edgeRenderers)
            { 
                if (lr != null) 
                    lr.enabled = visible; 
            }
        }

        if (instance2 != null)
        {
            foreach (var lr in instance2.edgeRenderers)
            { 
                if (lr != null) 
                    lr.enabled = visible; 
            }
        }
    }

    public void Refresh() { Rebuild(); }
}

public class DynamicFrustumTriggerHandler : MonoBehaviour
{
    private DynamicDualTrapezoPrismWireframe parent;
    private string targetTag;
    private string frustumName;

    public void Initialize(DynamicDualTrapezoPrismWireframe parentScript, string tag, string frustum)
    {
        parent = parentScript;
        targetTag = tag;
        frustumName = frustum;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (parent != null && other.CompareTag(targetTag))
        {
            parent.OnObjectEnteredFrustum(other.gameObject, frustumName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (parent != null && other.CompareTag(targetTag))
        {
            parent.OnObjectExitedFrustum(other.gameObject, frustumName);
        }
    }
}