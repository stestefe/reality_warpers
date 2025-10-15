using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class DestroyOnTrigger : MonoBehaviour
{

    public GameObject collisionEffectPrefab;

    [SerializeField] private AudioClip collectSound;

    private AudioSource audioSource;

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
            Debug.Log("AHLLLOAJOIJHOIJIOJOIJOIJOI"+ audioSource);
            audioSource.clip = collectSound;
            audioSource.Play();


            GameObject spawner = GameObject.FindObjectOfType<SpawnGameObject>()?.gameObject;
            if (spawner != null)
            {
                spawner.GetComponent<SpawnGameObject>().RemoveFromList(other.gameObject);
            }
        }
    }
}