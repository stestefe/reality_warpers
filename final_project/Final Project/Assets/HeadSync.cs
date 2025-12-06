using Unity.Netcode;
using UnityEngine;

public class HeadSync : NetworkBehaviour
{
    [SerializeField] private string centerEyeAnchorName = "CenterEyeAnchor";
    [SerializeField] private string mainCameraName = "Main Camera";

    private Transform localCenterEyeAnchor;
    private bool isInitialized = false;

    private GameObject visual;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            FindCenterEyeAnchor();

            if (localCenterEyeAnchor != null)
                isInitialized = true;
            else
                Debug.LogError("Head anchor not found.");
        }

        CreateVisual();
        // visual.SetActive(!IsOwner);
    }

    private void FindCenterEyeAnchor()
    {
        GameObject anchor = GameObject.Find(centerEyeAnchorName);
        if (anchor != null) {
            localCenterEyeAnchor = anchor.transform;
        } else {
            GameObject cameraAnchor = GameObject.Find(mainCameraName);
            localCenterEyeAnchor = cameraAnchor.transform;
        }

        
    }

    private void LateUpdate()
    {
        if (IsOwner && isInitialized)
        {
            transform.SetPositionAndRotation(
                localCenterEyeAnchor.position,
                localCenterEyeAnchor.rotation
            );
        }
    }

    private void CreateVisual()
    {
        visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * 0.2f;

        var r = visual.GetComponent<Renderer>();
        r.material = new Material(Shader.Find("Standard"))
        {
            color = new Color(0, 1, 0, 0.5f)
        };

        Destroy(visual.GetComponent<Collider>());
    }
}
