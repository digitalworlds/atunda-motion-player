using UnityEngine;

/// <summary>
/// When enabled, picks a random dance every few seconds.
/// Toggle from in-game UI or WebGL via WebBridge.
/// </summary>
public class RandomDanceMode : MonoBehaviour
{
    public static RandomDanceMode Instance { get; private set; }

    [SerializeField] float intervalSeconds = 4f;

    bool active;
    float nextSwitchTime;

    public bool IsActive => active;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (!active)
        {
            return;
        }

        if (Time.unscaledTime < nextSwitchTime)
        {
            return;
        }

        SwitchNow();
    }

    public void Toggle()
    {
        if (active)
        {
            StopMode();
        }
        else
        {
            StartMode();
        }
    }

    public void StartMode()
    {
        active = true;
        SwitchNow();
        Debug.Log("RandomDanceMode: started (every " + intervalSeconds + "s).");
    }

    public void StopMode()
    {
        active = false;
        Debug.Log("RandomDanceMode: stopped.");
    }

    /// <summary>Pick one random dance. Does not start auto-shuffle.</summary>
    public void PlayRandomNow()
    {
        active = false;
        AvatarUIItem.SelectRandomDance();
    }

    public void NotifyManualDanceChange()
    {
        if (active)
        {
            StopMode();
        }
    }

    void SwitchNow()
    {
        AvatarUIItem.SelectRandomDance();
        nextSwitchTime = Time.unscaledTime + Mathf.Max(0.5f, intervalSeconds);
    }

    public static RandomDanceMode EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RandomDanceMode existing = FindFirstObjectByType<RandomDanceMode>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject host = GameObject.Find("WebBridge");
        if (host == null)
        {
            host = new GameObject("RandomDanceMode");
        }

        return host.GetComponent<RandomDanceMode>() ?? host.AddComponent<RandomDanceMode>();
    }
}
