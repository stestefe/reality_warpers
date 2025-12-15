using UnityEngine;
using Unity.Netcode;

public class ClientBeamResponder : MonoBehaviour
{
    public GameObject objectToEnable;
    private ControllerBeam hostBeam;

    void Start()
    {
        StartCoroutine(FindHostBeam());
    }

    private System.Collections.IEnumerator FindHostBeam()
    {
        yield return new WaitForSeconds(0.5f);
        
        hostBeam = FindObjectOfType<ControllerBeam>();
        
        if (hostBeam != null)
        {
            hostBeam.isColliding.OnValueChanged += OnHostBeamCollisionChanged;
            
            OnHostBeamCollisionChanged(false, hostBeam.IsColliding);
            
            Debug.Log("Successfully connected to host's beam!");
        }
        else
        {
            Debug.LogWarning("Could not find ControllerBeam in scene");
        }
    }

    private void OnHostBeamCollisionChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"Client detected beam collision change: {newValue}");
        
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(newValue);
        }
    }

    void OnDestroy()
    {
        if (hostBeam != null)
        {
            hostBeam.isColliding.OnValueChanged -= OnHostBeamCollisionChanged;
        }
    }
}