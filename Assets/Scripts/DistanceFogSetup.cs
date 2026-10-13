using UnityEngine;

/// <summary>
/// Linear distance fog: clear near the camera, soft haze farther away.
/// </summary>
public class DistanceFogSetup : MonoBehaviour
{
    [SerializeField] Color fogColor = new Color(0.45f, 0.72f, 0.95f, 1f);
    [SerializeField] float fogStartDistance = 18f;
    [SerializeField] float fogEndDistance = 110f;

    void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = Mathf.Max(0f, fogStartDistance);
        RenderSettings.fogEndDistance = Mathf.Max(RenderSettings.fogStartDistance + 1f, fogEndDistance);

        Camera cam = Camera.main;
        if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
        {
            // Keep fog and clear color aligned so distant geometry blends into the sky.
            cam.backgroundColor = fogColor;
        }
    }

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<DistanceFogSetup>() != null)
        {
            return;
        }

        GameObject host = GameObject.Find("WebBridge");
        if (host == null)
        {
            host = new GameObject("DistanceFogSetup");
        }

        DistanceFogSetup setup = host.GetComponent<DistanceFogSetup>();
        if (setup == null)
        {
            setup = host.AddComponent<DistanceFogSetup>();
        }

        setup.Apply();
    }
}
