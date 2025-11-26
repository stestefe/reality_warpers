using UnityEngine;

public class CubCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("MainCamera"))
        {
            Debug.Log("Cube collided with MainCamera!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            Debug.Log("Cube triggered with MainCamera!");
        }
    }
}
