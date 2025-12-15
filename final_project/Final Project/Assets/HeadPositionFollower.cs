using UnityEngine;

public class HeadPositionFollower : MonoBehaviour
{
    public Transform headset;

    void LateUpdate()
    {
        transform.position = headset.position;
        transform.rotation = Quaternion.identity;
    }
}
