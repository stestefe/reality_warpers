using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);
    private NetworkVariable<int> winnerId = new NetworkVariable<int>(-1);

    private bool hostWinCondition1 = false;
    private bool hostWinCondition2 = false;

    private bool clientWinCondition = false;

    public delegate void GameEndedDelegate(int winner);
    public static event GameEndedDelegate OnGameEnded;

    public GameObject virtualFireworks;
    public GameObject physicalFireworks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        gameEnded.OnValueChanged += OnGameEndedChanged;
        winnerId.OnValueChanged += OnWinnerChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        gameEnded.OnValueChanged -= OnGameEndedChanged;
        winnerId.OnValueChanged -= OnWinnerChanged;
    }

    public void SetHostWinCondition1(bool value)
    {
        if (!IsServer) return;
        
        hostWinCondition1 = value;
        CheckWinConditions();
    }

    public void SetHostWinCondition2(bool value)
    {
        if (!IsServer) return;
        
        hostWinCondition2 = value;
        Debug.Log("winconditions2");
        CheckWinConditions();
    }

    public void SetClientWinCondition(bool value)
    {
        if (!IsServer) return;
        
        clientWinCondition = value;
        CheckWinConditions();
    }

    private void CheckWinConditions()
    {
        if (!IsServer) return;
        
        if (gameEnded.Value) return;

        // if (hostWinCondition1 && hostWinCondition2)
        if (hostWinCondition1)
        {
            StartCoroutine(SpawnFireworksForever(virtualFireworks));
            EndGame(0);
            return;
        }

        if (clientWinCondition)
        {
            StartCoroutine(SpawnFireworksForever(physicalFireworks));
            EndGame(1);
            return;
        }
    }

    private void EndGame(int winner)
    {
        if (!IsServer) return;

        gameEnded.Value = true;
        winnerId.Value = winner;

        Debug.Log($"Game Ended! Winner: {GetWinnerName(winner)}");
    }

    private void OnGameEndedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("Game has ended!");
            OnGameEnded?.Invoke(winnerId.Value);
        }
    }

    private void OnWinnerChanged(int previousValue, int newValue)
    {
        Debug.Log($"Winner is: {GetWinnerName(newValue)}");
    }

    public bool IsGameEnded() => gameEnded.Value;
    public int GetWinnerId() => winnerId.Value;
    
    public string GetWinnerName(int id)
    {
        return id switch
        {
            0 => "Host (Virtual Player)",
            1 => "Client (Physical Player)",
            _ => "No Winner"
        };
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetGameServerRpc()
    {
        gameEnded.Value = false;
        winnerId.Value = -1;
        hostWinCondition1 = false;
        hostWinCondition2 = false;
        clientWinCondition = false;
    }

    private IEnumerator SpawnFireworksForever(GameObject fireworks)
    {
        while (true)
        {
            for (int i = 0; i < 30; i++)
            {
                float x = UnityEngine.Random.Range(-7f, 7f);
                float y = UnityEngine.Random.Range(0f, 5f);
                float z = UnityEngine.Random.Range(-7f, 7f);
                Vector3 spawnPos = new Vector3(x, y, z);

                GameObject effect = Instantiate(fireworks, spawnPos, Quaternion.identity);
                Destroy(effect, 1f);
            }

            yield return new WaitForSeconds(2f);
        }
    }
}