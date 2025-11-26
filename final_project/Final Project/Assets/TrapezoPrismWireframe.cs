// TrapezoPrismWireframe.cs
// Creates a wireframe frustum-like trapezoidal prism that originates from the camera and moves with it.
// Only renders outer edges (no triangulation edges). Rotates only around Y-axis like a lighthouse.
// Includes collision detection for objects with specific tags.

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Transform))]
public class TrapezoPrismWireframe : MonoBehaviour
{
//     [Header("Frustum Dimensions")]
//     public float nearHalfWidth = 0.05f;
//     public float nearHalfHeight = 0.05f;
//     public float farHalfWidth = 0.3f;
//     public float farHalfHeight = 0.2f;
//     public float depth = 0.5f;

//     [Header("Appearance")]
//     public float lineWidth = 0.005f;
//     public Material lineMaterial;
//     public bool visibleInEditor = true;

//     [Header("Behavior")]
//     public bool parentToCenterEye = true;
//     public string[] candidateEyeNames = new string[] {"CenterEyeAnchor", "CenterEye"};

//     [Header("Collision Detection")]
//     public bool enableCollisionDetection = true;
//     public string targetTag = "Target";

//     private List<LineRenderer> edgeRenderers = new List<LineRenderer>();
//     private Transform wireParent;
//     private Transform targetEyeTransform;
//     private GameObject containerObject;
//     private GameObject edgesContainerObject;
//     private GameObject colliderObject;
//     private BoxCollider frustumCollider;
//     private List<GameObject> objectsInFrustum = new List<GameObject>();

//     private Vector3[] vertices = new Vector3[8];
    
//     private static readonly int[,] edges = new int[,]
//     {
//         {0,1},{1,2},{2,3},{3,0},
//         {4,5},{5,6},{6,7},{7,4},
//         {0,4},{1,5},{2,6},{3,7}
//     };

//     private void OnEnable() 
//     { 
//         CleanupAllOrphanedContainers();
//         FindAndCacheEyeTransform();
//         EnsureParentIfNeeded(); 
//         Rebuild(); 
//     }
    
//     private void OnDisable() 
//     { 
//         DestroyWireframeImmediate(); 
//         CleanupContainers();
//     }

//     private void OnDestroy()
//     {
//         DestroyWireframeImmediate();
//         CleanupContainers();
//     }

//     private void OnValidate() 
//     { 
//         if (!Application.isPlaying)
//         {
//             CleanupAllOrphanedContainers();
//         }
//         Rebuild(); 
//     }

//     private void Update() 
//     { 
//         if (!Application.isPlaying) 
//             SetWireVisibility(visibleInEditor); 
//     }

//     private void FindAndCacheEyeTransform()
//     {
//         if (!parentToCenterEye) return;
        
//         targetEyeTransform = FindCenterEyeTransform();
//         if (targetEyeTransform == null)
//         {
//             Debug.LogWarning("TrapezoPrismWireframe: Could not find CenterEyeAnchor. Frustum will not track camera.");
//         }
//     }

//     private void EnsureParentIfNeeded()
//     {
//         if (containerObject != null && wireParent != null)
//         {
//             return;
//         }

//         CleanupContainers();

//         if (!parentToCenterEye) 
//         { 
//             wireParent = transform; 
//             return; 
//         }

//         if (targetEyeTransform != null)
//         {
//             containerObject = new GameObject("FrustumWireframe_Container");
//             containerObject.transform.SetParent(targetEyeTransform, false);
//             containerObject.transform.localPosition = Vector3.zero;
//             containerObject.transform.localRotation = Quaternion.identity;
//             containerObject.transform.localScale = Vector3.one;
//             wireParent = containerObject.transform;
            
// #if UNITY_EDITOR
//             containerObject.hideFlags = HideFlags.DontSaveInEditor;
// #endif
//         }
//         else 
//         {
//             wireParent = transform;
//         }
//     }

//     private Transform FindCenterEyeTransform()
//     {
//         foreach (string n in candidateEyeNames)
//         {
//             GameObject go = GameObject.Find(n);
//             if (go != null) return go.transform;
//         }
//         return Camera.main != null ? Camera.main.transform : null;
//     }

//     private void Rebuild()
//     {
//         DestroyWireframeImmediate();
        
//         if (wireParent == null)
//         {
//             EnsureParentIfNeeded();
//         }

//         if (wireParent == null) return;

//         float cameraWorldY = wireParent.position.y;

//         vertices[0] = new Vector3(-nearHalfWidth, -cameraWorldY, 0f);
//         vertices[1] = new Vector3(nearHalfWidth, -cameraWorldY, 0f);
//         vertices[2] = new Vector3(nearHalfWidth, nearHalfHeight * 2f - cameraWorldY, 0f);
//         vertices[3] = new Vector3(-nearHalfWidth, nearHalfHeight * 2f - cameraWorldY, 0f);

//         vertices[4] = new Vector3(-farHalfWidth, -cameraWorldY, depth);
//         vertices[5] = new Vector3(farHalfWidth, -cameraWorldY, depth);
//         vertices[6] = new Vector3(farHalfWidth, farHalfHeight * 2f - cameraWorldY, depth);
//         vertices[7] = new Vector3(-farHalfWidth, farHalfHeight * 2f - cameraWorldY, depth);

//         GameObject edgesContainer = new GameObject("Frustum_Edges");
//         edgesContainer.transform.SetParent(wireParent, false);
//         edgesContainer.transform.localPosition = Vector3.zero;
//         edgesContainer.transform.localRotation = Quaternion.identity;
//         edgesContainer.transform.localScale = Vector3.one;
//         edgesContainerObject = edgesContainer;

// #if UNITY_EDITOR
//         edgesContainer.hideFlags = HideFlags.DontSaveInEditor;
// #endif

//         for (int e = 0; e < edges.GetLength(0); e++)
//         {
//             int i0 = edges[e,0]; 
//             int i1 = edges[e,1];
            
//             GameObject lineGO = new GameObject($"edge_{i0}_{i1}");
//             lineGO.transform.SetParent(edgesContainer.transform, false);
            
//             LineRenderer lr = lineGO.AddComponent<LineRenderer>();
//             lr.positionCount = 2;
//             lr.SetPosition(0, vertices[i0]);
//             lr.SetPosition(1, vertices[i1]);
//             lr.useWorldSpace = false;
//             lr.widthCurve = AnimationCurve.Constant(0f, 1f, lineWidth);
//             lr.alignment = LineAlignment.View;
//             lr.numCapVertices = 2;
//             lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
//             lr.receiveShadows = false;
            
//             if (lineMaterial != null) 
//             {
//                 lr.material = lineMaterial;
//             }
//             else 
//             { 
//                 Shader shader = Shader.Find("Unlit/Color"); 
//                 if (shader != null)
//                 { 
//                     Material m = new Material(shader); 
//                     m.SetColor("_Color", Color.green); 
//                     lr.material = m; 
//                 } 
//             }
            
//             edgeRenderers.Add(lr);
//         }

//         if (enableCollisionDetection && Application.isPlaying)
//         {
//             CreateFrustumCollider();
//         }
//     }

//     private void CreateFrustumCollider()
//     {
//         if (colliderObject != null)
//         {
//             if (Application.isPlaying)
//                 Destroy(colliderObject);
//             else
//                 DestroyImmediate(colliderObject);
//         }

//         colliderObject = new GameObject("Frustum_Collider");
//         colliderObject.transform.SetParent(wireParent, false);
//         colliderObject.transform.localPosition = new Vector3(0, 0, depth / 2f);
//         colliderObject.transform.localRotation = Quaternion.identity;
//         colliderObject.transform.localScale = Vector3.one;

//         Rigidbody rb = colliderObject.AddComponent<Rigidbody>();
//         rb.isKinematic = true;
//         rb.useGravity = false;
        
//         frustumCollider = colliderObject.AddComponent<BoxCollider>();
//         frustumCollider.isTrigger = true;
//         frustumCollider.size = new Vector3(farHalfWidth * 2f, farHalfHeight * 2f, depth);

//         FrustumTriggerHandler handler = colliderObject.AddComponent<FrustumTriggerHandler>();
//         handler.Initialize(this, targetTag);

// #if UNITY_EDITOR
//         colliderObject.hideFlags = HideFlags.DontSaveInEditor;
// #endif
//     }

//     public void OnObjectEnteredFrustum(GameObject obj)
//     {
//         if (!objectsInFrustum.Contains(obj))
//         {
//             objectsInFrustum.Add(obj);
//             Debug.Log($"Object entered frustum: {obj.name}");
//         }
//     }

//     public void OnObjectExitedFrustum(GameObject obj)
//     {
//         if (objectsInFrustum.Contains(obj))
//         {
//             objectsInFrustum.Remove(obj);
//             Debug.Log($"Object exited frustum: {obj.name}");
//         }
//     }

//     public List<GameObject> GetObjectsInFrustum()
//     {
//         return new List<GameObject>(objectsInFrustum);
//     }

//     public int GetObjectCount()
//     {
//         return objectsInFrustum.Count;
//     }

//     public bool IsObjectInFrustum(GameObject obj)
//     {
//         return objectsInFrustum.Contains(obj);
//     }

//     private void LateUpdate()
//     {
//         if (wireParent == null || !parentToCenterEye) return;

//         if (targetEyeTransform == null)
//         {
//             targetEyeTransform = FindCenterEyeTransform();
//         }

//         if (targetEyeTransform == null) return;

//         wireParent.position = targetEyeTransform.position;

//         Vector3 euler = targetEyeTransform.rotation.eulerAngles;
//         wireParent.rotation = Quaternion.Euler(0f, euler.y, 0f);

//         UpdateVerticesForGroundAlignment();
//     }

//     private void UpdateVerticesForGroundAlignment()
//     {
//         if (wireParent == null || edgeRenderers.Count == 0) return;

//         float cameraWorldY = wireParent.position.y;

//         vertices[0] = new Vector3(-nearHalfWidth, -cameraWorldY, 0f);
//         vertices[1] = new Vector3(nearHalfWidth, -cameraWorldY, 0f);
//         vertices[2] = new Vector3(nearHalfWidth, nearHalfHeight * 2f - cameraWorldY, 0f);
//         vertices[3] = new Vector3(-nearHalfWidth, nearHalfHeight * 2f - cameraWorldY, 0f);

//         vertices[4] = new Vector3(-farHalfWidth, -cameraWorldY, depth);
//         vertices[5] = new Vector3(farHalfWidth, -cameraWorldY, depth);
//         vertices[6] = new Vector3(farHalfWidth, farHalfHeight * 2f - cameraWorldY, depth);
//         vertices[7] = new Vector3(-farHalfWidth, farHalfHeight * 2f - cameraWorldY, depth);

//         int edgeIndex = 0;
//         for (int e = 0; e < edges.GetLength(0); e++)
//         {
//             if (edgeIndex >= edgeRenderers.Count) break;

//             LineRenderer lr = edgeRenderers[edgeIndex];
//             if (lr != null)
//             {
//                 int i0 = edges[e, 0];
//                 int i1 = edges[e, 1];
//                 lr.SetPosition(0, vertices[i0]);
//                 lr.SetPosition(1, vertices[i1]);
//             }
//             edgeIndex++;
//         }
//     }

//     private void DestroyWireframeImmediate()
//     {
//         foreach (var lr in edgeRenderers) 
//         { 
//             if (lr == null) continue; 
//             if (Application.isPlaying) 
//                 Destroy(lr.gameObject); 
//             else 
//                 DestroyImmediate(lr.gameObject); 
//         }
//         edgeRenderers.Clear();
        
//         if (edgesContainerObject != null)
//         {
//             if (Application.isPlaying)
//                 Destroy(edgesContainerObject);
//             else
//                 DestroyImmediate(edgesContainerObject);
//             edgesContainerObject = null;
//         }

//         if (colliderObject != null)
//         {
//             if (Application.isPlaying)
//                 Destroy(colliderObject);
//             else
//                 DestroyImmediate(colliderObject);
//             colliderObject = null;
//             frustumCollider = null;
//         }

//         objectsInFrustum.Clear();
//     }

//     private void CleanupContainers()
//     {
//         if (containerObject != null)
//         {
//             if (Application.isPlaying)
//                 Destroy(containerObject);
//             else
//                 DestroyImmediate(containerObject);
//             containerObject = null;
//         }
        
//         wireParent = null;
//     }

//     private void CleanupAllOrphanedContainers()
//     {
// #if UNITY_EDITOR
//         if (targetEyeTransform != null)
//         {
//             Transform[] children = targetEyeTransform.GetComponentsInChildren<Transform>(true);
//             foreach (Transform child in children)
//             {
//                 if (child.name == "FrustumWireframe_Container" && child != containerObject?.transform)
//                 {
//                     DestroyImmediate(child.gameObject);
//                 }
//             }
//         }
        
//         GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
//         foreach (GameObject obj in allObjects)
//         {
//             if ((obj.name == "Frustum_Edges" && obj != edgesContainerObject) ||
//                 (obj.name == "Frustum_Collider" && obj != colliderObject))
//             {
//                 DestroyImmediate(obj);
//             }
//         }
// #endif
//     }

//     private void SetWireVisibility(bool visible)
//     { 
//         foreach (var lr in edgeRenderers)
//         { 
//             if (lr != null) 
//                 lr.enabled = visible; 
//         } 
//     }

//     public void Refresh() { Rebuild(); }
// }

// public class FrustumTriggerHandler : MonoBehaviour
// {
//     private TrapezoPrismWireframe parent;
//     private string targetTag;

//     public void Initialize(TrapezoPrismWireframe parentScript, string tag)
//     {
//         parent = parentScript;
//         targetTag = tag;
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         if (parent != null && other.CompareTag(targetTag))
//         {
//             parent.OnObjectEnteredFrustum(other.gameObject);
//         }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (parent != null && other.CompareTag(targetTag))
//         {
//             parent.OnObjectExitedFrustum(other.gameObject);
//         }
//     }
}