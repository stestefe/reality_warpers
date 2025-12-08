using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerBeam : MonoBehaviour
{
    public Transform controller;
    public float beamLength = 1f;
    public Vector3 beamScale = new Vector3(0.05f, 0.05f, 1f);
    public Material beamMaterial; 

    public InputActionProperty triggerAction;

    private GameObject beamCube;

    void Start()
    {
        beamCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beamCube.transform.parent = controller;
        beamCube.transform.localRotation = Quaternion.identity;
        beamCube.transform.localScale = beamScale;
        beamCube.transform.localPosition = new Vector3(0, 0, beamScale.z / 2f);

        if (beamMaterial != null)
        {
            beamCube.GetComponent<Renderer>().material = beamMaterial;
        }

        beamCube.SetActive(false);

        triggerAction.action.Enable();
    }

    void Update()
    {
        bool triggerPressed = triggerAction.action.ReadValue<float>() > 0.1f;

        beamCube.SetActive(triggerPressed);

        if (triggerPressed)
        {
            beamCube.transform.localRotation = Quaternion.identity;
            beamCube.transform.localScale = beamScale;
            beamCube.transform.localPosition = new Vector3(0, 0, beamScale.z / 2f);
        }
    }
}
