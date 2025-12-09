using UnityEngine;
using Oculus.Interaction;

public class SnapTagFilter : MonoBehaviour, IGameObjectFilter
{
    [SerializeField]
    private string requiredTag = "SnapA";

    private SnapInteractable snapInteractable;

    private void Awake()
    {
        snapInteractable = GetComponent<SnapInteractable>();
        
        if (snapInteractable == null)
        {
            // Debug.LogError("SnapTagFilter requires a SnapInteractable component!", this);
            enabled = false;
            return;
        }

        var filters = snapInteractable.gameObject.GetComponents<IGameObjectFilter>();
    }

    public bool Filter(GameObject go)
    {
        bool hasTag = go.CompareTag(requiredTag);
        
        if (!hasTag)
        {
            // Debug.Log($"Object {go.name} cannot snap here - requires tag '{requiredTag}' but has '{go.tag}'");
        } 
        
        return hasTag;
    }
}