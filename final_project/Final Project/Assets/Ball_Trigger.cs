using UnityEngine;

public class BallTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            Debug.Log("Ball entered the trigger!");
            GameManager.Instance.SetHostWinCondition2(true);

        }
    }
}
