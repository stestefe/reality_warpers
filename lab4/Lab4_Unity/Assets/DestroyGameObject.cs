using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using TMPro;

public class DestroyOnTrigger : MonoBehaviour
{

    public GameObject collisionEffectPrefab;
    public GameObject beeCollisionEffectPrefab;

    public GameObject beeObject;

    private int flowersCollected = 0;

    [SerializeField] private AudioClip collectSound;

    private AudioSource audioSource;

    public TextMeshProUGUI girlText;
    public TextMeshProUGUI gameOverText;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Flower"))
        {
            Destroy(other.gameObject);
            Vector3 position = other.gameObject.transform.position;

            GameObject effect = Instantiate(collisionEffectPrefab, position, Quaternion.identity);

            Destroy(effect, 1f);
            audioSource.clip = collectSound;
            audioSource.Play();


            GameObject spawner = GameObject.FindObjectOfType<SpawnGameObject>()?.gameObject;
            if (spawner != null)
            {
                spawner.GetComponent<SpawnGameObject>().RemoveFromList(other.gameObject);
            }

            flowersCollected++;
            if (flowersCollected >= 3)
            {
                beeObject.SetActive(true);
            }
        }
        
        if (other.CompareTag("Bee"))
        {
            Destroy(other.gameObject);
            Vector3 position = other.gameObject.transform.position;

            GameObject effect = Instantiate(beeCollisionEffectPrefab, position, Quaternion.identity);
            girlText.gameObject.SetActive(false);
            gameOverText.gameObject.SetActive(true);
            Destroy(effect, 1f);
            audioSource.clip = collectSound;
            audioSource.Play();
        }
    }
}