using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ClientBeamResponder : MonoBehaviour
{
    public GameObject objectToEnable;
    public GameObject objectToEnable2;
    public float activeDuration = 10f;
    
    private ControllerBeam hostBeam;
    private Coroutine timedActivationCoroutine;

    void Start()
    {
        StartCoroutine(FindHostBeam());
    }

    private IEnumerator FindHostBeam()
    {
        yield return new WaitForSeconds(0.5f);
        
        hostBeam = FindObjectOfType<ControllerBeam>();
        
        if (hostBeam != null)
        {
            hostBeam.isColliding.OnValueChanged += OnHostBeamCollisionChanged;
            hostBeam.buttonTriggered.OnValueChanged += OnButtonTriggeredChanged;
            
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

    private void OnButtonTriggeredChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"Client detected button trigger: {newValue}");
        
        if (newValue && objectToEnable2 != null)
        {
            if (timedActivationCoroutine != null)
            {
                StopCoroutine(timedActivationCoroutine);
            }

            timedActivationCoroutine = StartCoroutine(ActivateForDuration());
            
            if (hostBeam != null && hostBeam.IsOwner)
            {
                hostBeam.buttonTriggered.Value = false;
            }
        }
    }

    private IEnumerator ActivateForDuration()
    {
        objectToEnable2.SetActive(true);
        Debug.Log($"Activated {objectToEnable2.name} for {activeDuration} seconds");
        
        yield return new WaitForSeconds(activeDuration);
        
        objectToEnable2.SetActive(false);
        Debug.Log($"Deactivated {objectToEnable2.name}");
        
        timedActivationCoroutine = null;
    }

    void OnDestroy()
    {
        if (hostBeam != null)
        {
            hostBeam.isColliding.OnValueChanged -= OnHostBeamCollisionChanged;
            hostBeam.buttonTriggered.OnValueChanged -= OnButtonTriggeredChanged; // New
        }
        
        // Clean up any running coroutines
        if (timedActivationCoroutine != null)
        {
            StopCoroutine(timedActivationCoroutine);
        }
    }
}