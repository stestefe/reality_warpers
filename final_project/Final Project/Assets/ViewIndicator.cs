using UnityEngine;

public class ViewIndicator : MonoBehaviour
{
    public Transform targetCamera;
    public float nearDist = 0.1f;
    public float farDist = 2f;
    public float halfAngle = 25f; 
    public LineRenderer line;

    void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.widthMultiplier = 0.01f;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = Color.cyan;
        line.useWorldSpace = false;
    }

    void Update()
    {
        if (!targetCamera) return;

        transform.position = targetCamera.position;
        transform.rotation = targetCamera.rotation;

        DrawFrustum();
    }

    void DrawFrustum()
    {
        float n = nearDist;
        float f = farDist;
        float wNear = Mathf.Tan(halfAngle * Mathf.Deg2Rad) * n;
        float wFar = Mathf.Tan(halfAngle * Mathf.Deg2Rad) * f;

        Vector3[] p = new Vector3[8];

        p[0] = new Vector3(-wNear,  wNear, n);
        p[1] = new Vector3( wNear,  wNear, n);
        p[2] = new Vector3( wNear, -wNear, n);
        p[3] = new Vector3(-wNear, -wNear, n);

        p[4] = new Vector3(-wFar,  wFar, f);
        p[5] = new Vector3( wFar,  wFar, f);
        p[6] = new Vector3( wFar, -wFar, f);
        p[7] = new Vector3(-wFar, -wFar, f);

        line.positionCount = 16;

        int i = 0;
        void E(int a, int b)
        {
            line.SetPosition(i++, p[a]);
            line.SetPosition(i++, p[b]);
        }

        E(0, 1); E(1, 2); E(2, 3); E(3, 0);
        E(4, 5); E(5, 6); E(6, 7); E(7, 4);
        E(0, 4); E(1, 5); E(2, 6); E(3, 7);
    }
}
