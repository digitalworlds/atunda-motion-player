using UnityEngine;

/// <summary>
/// Small gentle idle camera drift (position + yaw/pitch) around the starting pose.
/// </summary>
public class GentleCameraMotion : MonoBehaviour
{
    [SerializeField] float positionAmplitude = 0.045f;
    [SerializeField] float rotationAmplitudeDegrees = 0.55f;
    [SerializeField] float cycleSeconds = 9f;

    Vector3 basePosition;
    Quaternion baseRotation;
    float phaseX;
    float phaseY;
    float phaseYaw;
    float phasePitch;

    void Awake()
    {
        CaptureBasePose();
        phaseX = Random.Range(0f, Mathf.PI * 2f);
        phaseY = Random.Range(0f, Mathf.PI * 2f);
        phaseYaw = Random.Range(0f, Mathf.PI * 2f);
        phasePitch = Random.Range(0f, Mathf.PI * 2f);
    }

    void OnEnable()
    {
        CaptureBasePose();
    }

    void CaptureBasePose()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        float t = Time.unscaledTime * (Mathf.PI * 2f / Mathf.Max(0.1f, cycleSeconds));

        Vector3 offset = new Vector3(
            Mathf.Sin(t + phaseX) * positionAmplitude,
            Mathf.Sin(t * 0.85f + phaseY) * positionAmplitude * 0.65f,
            Mathf.Sin(t * 0.55f + phaseX) * positionAmplitude * 0.35f);

        float yaw = Mathf.Sin(t * 0.7f + phaseYaw) * rotationAmplitudeDegrees;
        float pitch = Mathf.Sin(t * 0.9f + phasePitch) * rotationAmplitudeDegrees * 0.7f;

        transform.localPosition = basePosition + offset;
        transform.localRotation = baseRotation * Quaternion.Euler(pitch, yaw, 0f);
    }

    public static void EnsureOnMainCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        if (cam.GetComponent<GentleCameraMotion>() == null)
        {
            cam.gameObject.AddComponent<GentleCameraMotion>();
        }
    }
}
