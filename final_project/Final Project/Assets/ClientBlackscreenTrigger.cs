using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;

public class ClientBlackscreenTrigger : NetworkBehaviour
{
    [Header("Input")]
    public InputActionProperty triggerButton;
    
    [Header("Cooldown Settings")]
    public float cooldownDuration = 10f;
    
    private BlackscreenController blackscreenController;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    void Start()
    {
        if (triggerButton.action != null)
        {
            triggerButton.action.Enable();
        }
        
        StartCoroutine(FindBlackscreenController());
    }

    private IEnumerator FindBlackscreenController()
    {
        yield return new WaitForSeconds(0.5f);
        
        blackscreenController = FindObjectOfType<BlackscreenController>();
        
        if (blackscreenController != null)
        {
            Debug.Log("Client found BlackscreenController");
        }
        else
        {
            Debug.LogWarning("Could not find BlackscreenController in scene");
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }
        }

        if (triggerButton.action != null && triggerButton.action.WasPressedThisFrame())
        {
            TriggerBlackscreen();
        }
    }

    private void TriggerBlackscreen()
    {
        if (isOnCooldown)
        {
            Debug.Log($"Blackscreen trigger on cooldown: {cooldownTimer:F1} seconds remaining");
            return;
        }

        if (blackscreenController == null)
        {
            Debug.LogWarning("BlackscreenController not found, attempting to find it again");
            blackscreenController = FindObjectOfType<BlackscreenController>();
            
            if (blackscreenController == null)
            {
                Debug.LogError("Cannot trigger blackscreen - controller not found");
                return;
            }
        }

        Debug.Log("Client requesting blackscreen activation from server");
        blackscreenController.ActivateBlackscreenServerRpc();

        isOnCooldown = true;
        cooldownTimer = cooldownDuration;
    }

    void OnDestroy()
    {
        if (triggerButton.action != null)
        {
            triggerButton.action.Disable();
        }
    }

    public float GetCooldownProgress()
    {
        if (!isOnCooldown) return 0f;
        return cooldownTimer / cooldownDuration;
    }

    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }

    public float GetCooldownTimeRemaining()
    {
        return cooldownTimer;
    }
}