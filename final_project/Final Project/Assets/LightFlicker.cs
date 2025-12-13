using UnityEngine;
using System.Collections;

public class LightToggleFlicker : MonoBehaviour
{
    public Light pointLight;

    public float minOnTime = 2f;
    public float maxOnTime = 6f;
    public float minOffTime = 0.05f;
    public float maxOffTime = 0.2f;

    void Start()
    {
        if (pointLight == null)
            pointLight = GetComponent<Light>();

        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            pointLight.enabled = true;
            yield return new WaitForSeconds(Random.Range(minOnTime, maxOnTime));

            pointLight.enabled = false;
            yield return new WaitForSeconds(Random.Range(minOffTime, maxOffTime));
        }
    }
}
