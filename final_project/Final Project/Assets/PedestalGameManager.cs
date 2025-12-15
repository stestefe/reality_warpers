using UnityEngine;

public class PedestalGameManager : MonoBehaviour
{
    public TriggerZone zone1;
    public TriggerZone zone2;
    public TriggerZone zone3;

    private bool gameCompleted = false;

    void Update()
    {
        if (!gameCompleted &&
            zone1.hasCorrectObject &&
            zone2.hasCorrectObject &&
            zone3.hasCorrectObject)
        {
            gameCompleted = true;
            GameManager.Instance.SetHostWinCondition1(true);
            Debug.Log("Game Completed!");
        }
    }
}
