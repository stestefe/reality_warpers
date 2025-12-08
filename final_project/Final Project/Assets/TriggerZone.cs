using UnityEngine;

public class TriggerZone : MonoBehaviour
{
    public string requiredTag;

    public bool hasCorrectObject = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(requiredTag))
        {
            hasCorrectObject = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(requiredTag))
        {
            hasCorrectObject = false;
        }
    }
}
