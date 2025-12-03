using Unity.Netcode;
using UnityEngine;

public class HeadSync : NetworkBehaviour
{
    [SerializeField] private string centerEyeAnchorName = "CenterEyeAnchor";
    
    private Transform localCenterEyeAnchor;
    private bool isInitialized = false;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            FindCenterEyeAnchor();
            
            if (localCenterEyeAnchor == null)
            {
                Debug.LogError("could not find target!");
            }
            else
            {
                Debug.Log("successfully found target");
                isInitialized = true;
            }
            // // TODO: comment out later
            CreateVisualRepresentation();
        }
        else
        {
            Debug.Log("displaying remote head");
            
            CreateVisualRepresentation();
        }
    }

    private void FindCenterEyeAnchor()
    {
        GameObject eyeAnchor = GameObject.Find(centerEyeAnchorName);
        if (eyeAnchor != null)
        {
            localCenterEyeAnchor = eyeAnchor.transform;
            return;
        }

        OVRCameraRig ovrRig = FindObjectOfType<OVRCameraRig>();
        if (ovrRig != null)
        {
            localCenterEyeAnchor = ovrRig.centerEyeAnchor;
            return;
        }

        // if (Camera.main != null)
        // {
        //     localCenterEyeAnchor = Camera.main.transform;
        // }
    }

    private void LateUpdate()
    {
        if (IsOwner && isInitialized && localCenterEyeAnchor != null)
        {
            transform.position = localCenterEyeAnchor.position;
            transform.rotation = localCenterEyeAnchor.rotation;
        }
    }

    private void CreateVisualRepresentation()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * 0.2f; 
        
        Renderer renderer = sphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0, 1, 0, 0.5f); 
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        renderer.material = mat;
        Destroy(sphere.GetComponent<Collider>());
    }
}