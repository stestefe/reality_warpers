using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;
using System.Linq;

/// <summary>
/// Manages a snap puzzle - checks when all pedestals are correctly filled
/// Attach to an empty GameObject as the puzzle manager
/// </summary>
public class SnapPuzzleManager : MonoBehaviour
{
    [Header("Pedestal References")]
    [SerializeField]
    [Tooltip("All pedestals that need to be filled")]
    private SnapInteractable[] pedestals;

    [Header("Win Conditions")]
    [SerializeField]
    [Tooltip("Check if puzzle is complete on Start (useful for testing)")]
    private bool checkOnStart = false;

    [Header("Events")]
    [SerializeField]
    [Tooltip("Called when puzzle is completed")]
    private UnityEvent onPuzzleComplete;

    [SerializeField]
    [Tooltip("Called when an object is snapped (any pedestal)")]
    private UnityEvent onObjectSnapped;

    [SerializeField]
    [Tooltip("Called when an object is unsnapped (any pedestal)")]
    private UnityEvent onObjectUnsnapped;

    private bool puzzleCompleted = false;
    private int filledPedestals = 0;

    private void Start()
    {
        if (pedestals == null || pedestals.Length == 0)
        {
            pedestals = FindObjectsOfType<SnapInteractable>();
            Debug.Log($"Auto-found {pedestals.Length} pedestals");
        }

        foreach (var pedestal in pedestals)
        {
            if (pedestal != null)
            {
                pedestal.WhenSelectingInteractorViewAdded += OnObjectSnapped;
                pedestal.WhenSelectingInteractorViewRemoved += OnObjectUnsnapped;
            }
        }

        if (checkOnStart)
        {
            CheckPuzzleCompletion();
        }
    }

    private void OnDestroy()
    {
        foreach (var pedestal in pedestals)
        {
            if (pedestal != null)
            {
                pedestal.WhenSelectingInteractorViewAdded -= OnObjectSnapped;
                pedestal.WhenSelectingInteractorViewRemoved -= OnObjectUnsnapped;
            }
        }
    }

    private void OnObjectSnapped(IInteractorView view)
    {
        Debug.Log($"Object snapped! Checking puzzle completion...");
        onObjectSnapped?.Invoke();
        CheckPuzzleCompletion();
    }

    private void OnObjectUnsnapped(IInteractorView view)
    {
        Debug.Log("Object unsnapped");
        onObjectUnsnapped?.Invoke();
        
        if (puzzleCompleted)
        {
            puzzleCompleted = false;
            Debug.Log("Puzzle no longer complete - object was removed");
        }
    }

    private void CheckPuzzleCompletion()
    {
        if (puzzleCompleted)
            return;
            
        filledPedestals = 0;
        
        foreach (var pedestal in pedestals)
        {
            if (pedestal != null && IsPedestalFilled(pedestal))
            {
                filledPedestals++;
            }
        }

        Debug.Log($"Pedestals filled: {filledPedestals}/{pedestals.Length}");

        if (filledPedestals == pedestals.Length && pedestals.Length > 0)
        {
            CompletePuzzle();
        }
    }

    private bool IsPedestalFilled(SnapInteractable pedestal)
    {
        return pedestal.SelectingInteractorViews.Any();
    }

    private void CompletePuzzle()
    {
        puzzleCompleted = true;
        Debug.Log("PUZZLE COMPLETE! All objects correctly placed!");
        
        onPuzzleComplete?.Invoke();
        
    }

    public void ManualCheck()
    {
        CheckPuzzleCompletion();
    }

    public void ResetPuzzle()
    {
        puzzleCompleted = false;
        filledPedestals = 0;
        Debug.Log("Puzzle reset");
    }

    public float GetProgress()
    {
        if (pedestals.Length == 0) return 0f;
        return (float)filledPedestals / pedestals.Length;
    }
}