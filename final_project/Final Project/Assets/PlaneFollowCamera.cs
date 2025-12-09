using UnityEngine;

public class PlaneFollowCamera : MonoBehaviour
{
    public Transform vrCamera;
    public Vector3 offset = new Vector3(0, 0, 1f); 

    void Update()
    {
        transform.position = vrCamera.position + vrCamera.forward * offset.z;
        transform.rotation = vrCamera.rotation;
    }
}
