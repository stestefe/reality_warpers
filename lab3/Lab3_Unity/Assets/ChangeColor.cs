using UnityEngine;
using Oculus.Interaction;

public class ChangeColor : MonoBehaviour
{
    [SerializeField]  
    Color selectedColor = Color.yellow;

    [SerializeField]
    Color originalColor;

    [SerializeField]
    Color magenta = Color.magenta;

    private Renderer objectRenderer;
    private Grabbable grabbable;
    private bool isColorStored = false;

    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }

        grabbable = GetComponent<Grabbable>();

        if (objectRenderer != null && !isColorStored)
        {
            originalColor = objectRenderer.material.color;
            isColorStored = true;
        }
    }

    void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
    }

    void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (objectRenderer == null) return;

        switch (evt.Type)
        {
            case PointerEventType.Select:
                objectRenderer.material.color = selectedColor;
                break;
            case PointerEventType.Unselect:
                objectRenderer.material.color = originalColor;
                break;
            case PointerEventType.Hover:
                objectRenderer.material.color = magenta;
                break;
            case PointerEventType.Unhover:
                objectRenderer.material.color = originalColor;
                break;
        }
    }
}