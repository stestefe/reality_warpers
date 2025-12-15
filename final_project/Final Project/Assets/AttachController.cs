using UnityEngine;

public class AttachController : MonoBehaviour
{
    public Transform rightController;
    public Transform window;

    public float maxDistance = 0.2f;

    void Update()
    {
        Ray ray = new Ray(rightController.position, rightController.forward);
        // RaycastHit hit;

        // if (Physics.Raycast(ray, out hit, maxDistance))
        // {
        //     window.position = hit.point;
        //     window.rotation = Quaternion.LookRotation(hit.normal);
        // }
        // else
        // {
            window.position = ray.origin + ray.direction * maxDistance;
            window.rotation = Quaternion.LookRotation(ray.direction, Vector3.up);
        // }
    }
}
