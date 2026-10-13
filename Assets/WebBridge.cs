using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Public entry points for JavaScript (WebGL) via
/// unityInstance.SendMessage("WebBridge", "MethodName").
/// </summary>
public class WebBridge : MonoBehaviour
{
    public static bool DanceListUiVisible { get; private set; } = true;
    public static bool GameUiVisible { get; private set; } = true;

    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
        RandomDanceMode.EnsureExists();
        GentleCameraMotion.EnsureOnMainCamera();
        InGameRandomDanceButton.EnsureExists();
        InGamePausePlayButton.EnsureExists();
        DistanceFogSetup.EnsureExists();
        if (ShouldHideGameUiFromUrl())
        {
            GameUiVisible = false;
        }
    }

    void Start()
    {
        if (!GameUiVisible)
        {
            ApplyGameUiVisible(false);
        }

        StartCoroutine(ReportCharactersSoon());
    }

    IEnumerator ReportCharactersSoon()
    {
        yield return null;
        yield return null;
        if (!GameUiVisible)
        {
            ApplyGameUiVisible(false);
        }

        ReportCharacters("");
    }

    /// <summary>
    /// Same behavior as the in-game right arrow: advance to the next character.
    /// </summary>
    public void NextCharacter()
    {
        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null)
        {
            Debug.LogWarning("WebBridge.NextCharacter: no CharacterSelector found.");
            return;
        }

        selector.NextCharacter();
        ReportCharacters("");
        Debug.Log("WebBridge.NextCharacter: advanced character.");
    }

    /// <summary>
    /// Same behavior as the in-game left arrow: go to the previous character.
    /// </summary>
    public void PreviousCharacter()
    {
        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null)
        {
            Debug.LogWarning("WebBridge.PreviousCharacter: no CharacterSelector found.");
            return;
        }

        selector.PreviousCharacter();
        ReportCharacters("");
        Debug.Log("WebBridge.PreviousCharacter: previous character.");
    }

    /// <summary>
    /// Selects the next dance in the menu list.
    /// If nothing is selected, selects the first dance.
    /// </summary>
    public void NextDanceMove()
    {
        RandomDanceMode.EnsureExists().NotifyManualDanceChange();
        if (AvatarUIItem.SelectNextDance())
        {
            Debug.Log("WebBridge.NextDanceMove: selected next dance.");
            return;
        }

        Debug.LogWarning("WebBridge.NextDanceMove: no dance menu items found.");
    }

    /// <summary>
    /// Selects the previous dance in the menu list.
    /// If nothing is selected, selects the last dance.
    /// </summary>
    public void PreviousDanceMove()
    {
        RandomDanceMode.EnsureExists().NotifyManualDanceChange();
        if (AvatarUIItem.SelectPreviousDance())
        {
            Debug.Log("WebBridge.PreviousDanceMove: selected previous dance.");
            return;
        }

        Debug.LogWarning("WebBridge.PreviousDanceMove: no dance menu items found.");
    }

    /// <summary>
    /// Pick one random dance immediately. Does not auto-cycle.
    /// </summary>
    public void ToggleRandomDanceMode()
    {
        RandomDanceMode.EnsureExists().PlayRandomNow();
    }

    public void StartRandomDanceMode()
    {
        RandomDanceMode.EnsureExists().StartMode();
    }

    public void StopRandomDanceMode()
    {
        RandomDanceMode.EnsureExists().StopMode();
    }

    /// <summary>
    /// Hide the whole in-game overlay: dance list, titles, random, left/right, and pause.
    /// The 3D scene stays; host pages can drive it with SendMessage.
    /// </summary>
    public void ToggleGameUi()
    {
        SetGameUiVisible(!GameUiVisible);
    }

    public void ShowGameUi()
    {
        SetGameUiVisible(true);
    }

    public void HideGameUi()
    {
        SetGameUiVisible(false);
    }

    /// <summary>JS SendMessage string: "1"/"true"/"on" to show, anything else to hide.</summary>
    public void SetGameUiVisible(string visible)
    {
        SetGameUiVisible(ParseVisibleFlag(visible));
    }

    void SetGameUiVisible(bool visible)
    {
        GameUiVisible = visible;
        if (!ApplyGameUiVisible(visible))
        {
            Debug.LogWarning("WebBridge.SetGameUiVisible: in-game UI not found.");
            return;
        }

        Debug.Log("WebBridge.SetGameUiVisible: " + visible);
    }

    static bool ApplyGameUiVisible(bool visible)
    {
        bool found = false;
        GameObject canvas = FindGameUiCanvas();
        if (canvas != null)
        {
            SetCanvasGroupVisible(canvas, visible);
            found = true;
        }

        GameObject[] targets = FindGameUiHideTargets();
        for (int i = 0; i < targets.Length; i++)
        {
            SetCanvasGroupVisible(targets[i], visible);
            found = true;
        }

        return found;
    }

    /// <summary>
    /// Same behavior as the in-game pause / play button.
    /// </summary>
    public void TogglePause()
    {
        InGamePausePlayButton.TogglePlayback();
    }

    public void PauseDance()
    {
        InGamePausePlayButton.PausePlayback();
    }

    public void ResumeDance()
    {
        InGamePausePlayButton.ResumePlayback();
    }

    /// <summary>
    /// Fully stop the current dance. Use TogglePause to freeze and resume.
    /// </summary>
    public void StopDance()
    {
        RandomDanceMode.EnsureExists().NotifyManualDanceChange();
        AvatarAnimationPlayer[] players = FindObjectsByType<AvatarAnimationPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int stopped = 0;
        for (int i = 0; i < players.Length; i++)
        {
            AvatarAnimationPlayer player = players[i];
            if (player == null || player.gameObject.name.StartsWith("Preview", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            player.StopDance();
            stopped++;
        }

        if (stopped == 0)
        {
            Debug.LogWarning("WebBridge.StopDance: no dancer found.");
            return;
        }

        Debug.Log("WebBridge.StopDance: stopped " + stopped + " dancer(s).");
    }

    /// <summary>
    /// Hide or show the dance list, random, left/right arrows, and pause.
    /// Title text, avatar, and background stay visible.
    /// </summary>
    public void ToggleDanceList()
    {
        SetDanceListVisible(!IsDanceListVisible());
    }

    public void ShowDanceList()
    {
        SetDanceListVisible(true);
    }

    public void HideDanceList()
    {
        SetDanceListVisible(false);
    }

    /// <summary>JS SendMessage string: "1"/"true"/"on" to show, anything else to hide.</summary>
    public void SetDanceListVisible(string visible)
    {
        SetDanceListVisible(ParseVisibleFlag(visible));
    }

    void SetDanceListVisible(bool visible)
    {
        GameObject[] targets = FindDanceListHideTargets();
        if (targets.Length == 0)
        {
            Debug.LogWarning("WebBridge.SetDanceListVisible: dance list UI not found.");
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            SetCanvasGroupVisible(targets[i], visible);
        }

        DanceListUiVisible = visible;
        Debug.Log("WebBridge.SetDanceListVisible: " + visible);
    }

    static bool IsDanceListVisible()
    {
        GameObject scrollView = FindDanceScrollView();
        if (scrollView == null)
        {
            return true;
        }

        CanvasGroup group = scrollView.GetComponent<CanvasGroup>();
        if (group == null)
        {
            return scrollView.activeSelf;
        }

        return group.alpha > 0.01f && group.blocksRaycasts;
    }

    static GameObject[] FindDanceListHideTargets()
    {
        System.Collections.Generic.List<GameObject> targets = new System.Collections.Generic.List<GameObject>();
        GameObject scrollView = FindDanceScrollView();
        if (scrollView != null)
        {
            targets.Add(scrollView);
        }

        AddNamedTarget(targets, "RandomDanceButton");
        AddNamedTarget(targets, "Left_Arrow");
        AddNamedTarget(targets, "Right_Arrow");
        AddNamedTarget(targets, "Stop_Dancing");
        return targets.ToArray();
    }

    static void AddNamedTarget(System.Collections.Generic.List<GameObject> targets, string name)
    {
        GameObject go = FindNamedUi(name);
        if (go != null && !targets.Contains(go))
        {
            targets.Add(go);
        }
    }

    static GameObject FindNamedUi(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
        {
            return go;
        }

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t != null && t.name == name)
            {
                return t.gameObject;
            }
        }

        return null;
    }

    static void SetCanvasGroupVisible(GameObject go, bool visible)
    {
        if (go == null)
        {
            return;
        }

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = go.AddComponent<CanvasGroup>();
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;

        Image image = go.GetComponent<Image>();
        if (image != null)
        {
            image.enabled = visible;
        }

        Button button = go.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = visible;
        }
    }

    static GameObject FindDanceScrollView()
    {
        DanceMenuGenerator menu = FindFirstObjectByType<DanceMenuGenerator>();
        if (menu != null)
        {
            ScrollRect scrollRect = menu.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                return scrollRect.gameObject;
            }
        }

        return GameObject.Find("Scroll View");
    }

    static GameObject FindGameUiCanvas()
    {
        GameObject leftArrow = GameObject.Find("Left_Arrow");
        if (leftArrow != null)
        {
            Canvas canvas = leftArrow.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                return canvas.gameObject;
            }
        }

        GameObject named = GameObject.Find("Canvas (1)");
        if (named != null)
        {
            return named;
        }

        Canvas overlay = FindOverlayCanvas();
        return overlay != null ? overlay.gameObject : null;
    }

    static Canvas FindOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Canvas best = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isActiveAndEnabled)
            {
                continue;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvas;
            }

            if (best == null)
            {
                best = canvas;
            }
        }

        return best;
    }

    static GameObject[] FindGameUiHideTargets()
    {
        string[] names =
        {
            "Left_Arrow",
            "Right_Arrow",
            "Stop_Dancing",
            "RandomDanceButton",
            "Dance_Text",
            "Character_Text",
            "Scroll View"
        };

        System.Collections.Generic.List<GameObject> targets = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < names.Length; i++)
        {
            AddNamedTarget(targets, names[i]);
        }

        GameObject scrollView = FindDanceScrollView();
        if (scrollView != null && !targets.Contains(scrollView))
        {
            targets.Add(scrollView);
        }

        return targets.ToArray();
    }

    static bool ShouldHideGameUiFromUrl()
    {
        string url = Application.absoluteURL;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        string lower = url.ToLowerInvariant();
        return ContainsQueryFlag(lower, "hideui")
            || ContainsQueryFlag(lower, "hidegameui")
            || lower.Contains("gameui=0")
            || lower.Contains("ui=0");
    }

    static bool ContainsQueryFlag(string url, string key)
    {
        return url.Contains(key + "=1")
            || url.Contains(key + "=true")
            || url.Contains(key + "=on")
            || url.Contains(key + "=hide");
    }

    static bool ParseVisibleFlag(string visible)
    {
        if (string.IsNullOrWhiteSpace(visible))
        {
            return false;
        }

        string value = visible.Trim();
        return value == "1"
            || value.Equals("true", System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("on", System.StringComparison.OrdinalIgnoreCase)
            || value.Equals("show", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Change the on-screen name of the currently selected dance.
    /// Playback still uses the original dance file id.
    /// </summary>
    public void RenameCurrentDance(string displayName)
    {
        displayName = SanitizeDisplayName(displayName);
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("WebBridge.RenameCurrentDance: empty name.");
            return;
        }

        if (!AvatarUIItem.RenameSelectedDance(displayName))
        {
            Debug.LogWarning("WebBridge.RenameCurrentDance: no dance to rename.");
            return;
        }

        Debug.Log("WebBridge.RenameCurrentDance: '" + displayName + "'.");
    }

    /// <summary>
    /// Remove the currently selected dance from the in-game list for this session.
    /// Built-in dances come back after a page refresh. Keeps at least one dance.
    /// </summary>
    public void RemoveCurrentDance(string unused)
    {
        RandomDanceMode.EnsureExists().NotifyManualDanceChange();
        if (!AvatarUIItem.RemoveSelectedDance())
        {
            Debug.LogWarning("WebBridge.RemoveCurrentDance: could not remove dance.");
            return;
        }

        Debug.Log("WebBridge.RemoveCurrentDance: removed current dance.");
    }

    /// <summary>
    /// Change the on-screen name of the current character.
    /// </summary>
    public void RenameCurrentAvatar(string displayName)
    {
        displayName = SanitizeDisplayName(displayName);
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("WebBridge.RenameCurrentAvatar: empty name.");
            return;
        }

        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null || !selector.RenameCurrentAvatar(displayName))
        {
            Debug.LogWarning("WebBridge.RenameCurrentAvatar: no character to rename.");
            return;
        }

        Debug.Log("WebBridge.RenameCurrentAvatar: '" + displayName + "'.");
        ReportCharacters("");
    }

    public void SelectCharacter(string indexText)
    {
        if (!int.TryParse(indexText, out int index))
        {
            Debug.LogWarning("WebBridge.SelectCharacter: expected an index.");
            return;
        }

        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null || !selector.SelectCharacter(index))
        {
            Debug.LogWarning("WebBridge.SelectCharacter: could not select character " + indexText + ".");
            return;
        }

        ReportCharacters("");
        Debug.Log("WebBridge.SelectCharacter: " + index + ".");
    }

    public void RemoveCurrentAvatar(string unused)
    {
        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null || !selector.RemoveCurrentAvatar())
        {
            Debug.LogWarning("WebBridge.RemoveCurrentAvatar: could not remove character.");
            return;
        }

        ReportCharacters("");
        Debug.Log("WebBridge.RemoveCurrentAvatar: removed current character.");
    }

    public void ReportCharacters(string unused)
    {
        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null)
        {
            return;
        }

        PushCharacterListToPage(
            selector.GetCharacterNamesJoined(),
            selector.CurrentCharacterIndex,
            selector.BuiltInCount);
    }

    public void RemoveCharacter(string indexText)
    {
        if (!int.TryParse(indexText, out int index))
        {
            Debug.LogWarning("WebBridge.RemoveCharacter: expected an index.");
            return;
        }

        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null || !selector.RemoveAvatarAt(index))
        {
            Debug.LogWarning("WebBridge.RemoveCharacter: could not remove character " + indexText + ".");
            return;
        }

        ReportCharacters("");
        Debug.Log("WebBridge.RemoveCharacter: removed " + index + ".");
    }

    static void PushCharacterListToPage(string names, int selectedIndex, int builtInCount)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        AtundaReportCharacters(names ?? "", selectedIndex, builtInCount);
#endif
    }

    /// <summary>
    /// Add a new avatar from a GLB URL.
    /// Payload: JSON {"name":"...","url":"..."} or "name\nurl".
    /// </summary>
    public void LoadAvatarFromUrl(string payload)
    {
        if (!TryParseNameAndUrl(payload, out string avatarName, out string url, out string displayName))
        {
            Debug.LogWarning("WebBridge.LoadAvatarFromUrl: expected name and url.");
            return;
        }

        if (!string.IsNullOrEmpty(displayName))
        {
            avatarName = displayName;
        }

        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null)
        {
            Debug.LogWarning("WebBridge.LoadAvatarFromUrl: no CharacterSelector found.");
            return;
        }

        byte[] localBytes = TryReadLocalGlbBytes(url);
        if (!selector.AddExternalAvatar(avatarName, url, localBytes))
        {
            Debug.LogWarning("WebBridge.LoadAvatarFromUrl: failed to add '" + avatarName + "'.");
            return;
        }

        ReportCharacters("");
    }

    /// <summary>
    /// Add a new avatar from a GLB the website stored in JavaScript (file picker).
    /// </summary>
    public void LoadAvatarFromPendingFile(string avatarName)
    {
        string displayName = SanitizeDisplayName(avatarName);
        avatarName = SanitizeDanceName(avatarName);
        if (string.IsNullOrEmpty(avatarName))
        {
            avatarName = "CustomAvatar";
        }

        if (!string.IsNullOrEmpty(displayName))
        {
            avatarName = displayName;
        }

        byte[] glbBytes = ConsumePendingAvatarBytes();
        if (glbBytes == null || glbBytes.Length == 0)
        {
            Debug.LogWarning("WebBridge.LoadAvatarFromPendingFile: no GLB bytes were provided.");
            return;
        }

        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector == null)
        {
            Debug.LogWarning("WebBridge.LoadAvatarFromPendingFile: no CharacterSelector found.");
            return;
        }

        if (!selector.AddExternalAvatar(avatarName, "", glbBytes))
        {
            Debug.LogWarning("WebBridge.LoadAvatarFromPendingFile: failed to add '" + avatarName + "'.");
            return;
        }

        ReportCharacters("");
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern int GetPendingAvatarGlbLength();
    [DllImport("__Internal")] static extern int ConsumePendingAvatarGlb(System.IntPtr dest, int maxLen);
    [DllImport("__Internal")] static extern int GetPendingDanceJsonLength();
    [DllImport("__Internal")] static extern int ConsumePendingDanceJson(System.IntPtr dest, int maxLen);
    [DllImport("__Internal")] static extern void AtundaReportCharacters(string names, int selectedIndex, int builtInCount);
#endif

    static byte[] ConsumePendingAvatarBytes()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        int length = GetPendingAvatarGlbLength();
        if (length <= 0)
        {
            return null;
        }

        byte[] data = new byte[length];
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            ConsumePendingAvatarGlb(handle.AddrOfPinnedObject(), length);
        }
        finally
        {
            handle.Free();
        }

        return data;
#else
        return null;
#endif
    }

    static byte[] ConsumePendingDanceBytes()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        int length = GetPendingDanceJsonLength();
        if (length <= 0)
        {
            return null;
        }

        byte[] data = new byte[length];
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            ConsumePendingDanceJson(handle.AddrOfPinnedObject(), length);
        }
        finally
        {
            handle.Free();
        }

        return data;
#else
        return null;
#endif
    }

    static byte[] TryReadLocalGlbBytes(string url)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return null;
#else
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        string path = url.Trim();
        if (path.StartsWith("file://", System.StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring("file://".Length);
        }

        if (path.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!System.IO.File.Exists(path))
        {
            return null;
        }

        return System.IO.File.ReadAllBytes(path);
#endif
    }

    /// <summary>
    /// Load a dance JSON from a URL and play it.
    /// Payload: JSON {"name":"...","url":"..."} or "name\nurl".
    /// </summary>
    public void LoadDanceFromUrl(string payload)
    {
        if (!TryParseNameAndUrl(payload, out string danceName, out string url, out string displayName))
        {
            Debug.LogWarning("WebBridge.LoadDanceFromUrl: expected name and url.");
            return;
        }

        RandomDanceMode.EnsureExists().NotifyManualDanceChange();
        NpzMotionClipCache.Release("atunda", danceName);
        StopCoroutine(nameof(LoadDanceFromUrlRoutine));
        StartCoroutine(LoadDanceFromUrlRoutine(danceName, url, displayName));
    }

    IEnumerator LoadDanceFromUrlRoutine(string danceName, string url, string displayName)
    {
        bool loaded = false;
        yield return NpzMotionClipCache.LoadFromUrlAsync(danceName, url, true, ok => loaded = ok);
        if (!loaded)
        {
            Debug.LogWarning("WebBridge.LoadDanceFromUrl: failed to load '" + danceName + "' from " + url);
            yield break;
        }

        DanceMenuGenerator menu = FindFirstObjectByType<DanceMenuGenerator>();
        if (menu != null && menu.AddOrSelectExternalDance(danceName, displayName))
        {
            Debug.Log("WebBridge.LoadDanceFromUrl: playing '" + danceName + "'.");
            yield break;
        }

        PlayLoadedExternalDance(danceName, displayName);
    }

    /// <summary>
    /// Play a dance JSON the website stored in JavaScript (file picker).
    /// </summary>
    public void LoadDanceFromPendingFile(string danceName)
    {
        string displayName = SanitizeDisplayName(danceName);
        danceName = SanitizeDanceName(danceName);
        if (string.IsNullOrEmpty(danceName))
        {
            danceName = "CustomDance";
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = danceName;
        }

        byte[] jsonBytes = ConsumePendingDanceBytes();
        if (jsonBytes == null || jsonBytes.Length == 0)
        {
            Debug.LogWarning("WebBridge.LoadDanceFromPendingFile: no JSON bytes were provided.");
            return;
        }

        string jsonText = System.Text.Encoding.UTF8.GetString(jsonBytes);
        if (jsonText.Length > 0 && jsonText[0] == '\uFEFF')
        {
            jsonText = jsonText.Substring(1);
        }

        RandomDanceMode.EnsureExists().NotifyManualDanceChange();
        NpzMotionClipCache.Release("atunda", danceName);
        if (!NpzMotionClipCache.TryStoreFromJsonText(danceName, jsonText, true))
        {
            Debug.LogWarning("WebBridge.LoadDanceFromPendingFile: failed to parse '" + danceName + "'.");
            return;
        }

        PlayLoadedExternalDance(danceName, displayName);
    }

    void PlayLoadedExternalDance(string danceName, string displayName = null)
    {
        DanceMenuGenerator menu = FindFirstObjectByType<DanceMenuGenerator>();
        if (menu != null && menu.AddOrSelectExternalDance(danceName, displayName))
        {
            Debug.Log("WebBridge: playing '" + danceName + "'.");
            return;
        }

        AvatarAnimationPlayer player = FindActiveMainDancer();
        if (player != null)
        {
            player.PlayDance(danceName);
            Debug.Log("WebBridge: playing '" + danceName + "' without menu tile.");
            return;
        }

        Debug.LogWarning("WebBridge: loaded clip but no dancer was found.");
    }

    static AvatarAnimationPlayer FindActiveMainDancer()
    {
        AvatarAnimationPlayer[] players = FindObjectsByType<AvatarAnimationPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            AvatarAnimationPlayer player = players[i];
            if (player != null && player.gameObject.activeInHierarchy
                && !player.gameObject.name.StartsWith("Preview", System.StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }
        }

        return players.Length > 0 ? players[0] : null;
    }

    static bool TryParseNameAndUrl(string payload, out string danceName, out string url, out string displayName)
    {
        danceName = null;
        url = null;
        displayName = null;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        string trimmed = payload.Trim();
        if (trimmed.StartsWith("{", System.StringComparison.Ordinal))
        {
            try
            {
                UrlDancePayload parsed = JsonUtility.FromJson<UrlDancePayload>(trimmed);
                danceName = parsed != null ? parsed.name : null;
                url = parsed != null ? parsed.url : null;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("WebBridge.LoadDanceFromUrl: JSON parse failed: " + ex.Message);
            }
        }

        if (string.IsNullOrWhiteSpace(danceName) || string.IsNullOrWhiteSpace(url))
        {
            string[] parts = trimmed.Split(new[] { '\n' }, 2);
            if (parts.Length == 2)
            {
                danceName = parts[0].Trim();
                url = parts[1].Trim();
            }
        }

        displayName = SanitizeDisplayName(danceName);
        danceName = SanitizeDanceName(danceName);
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = danceName;
        }

        url = url != null ? url.Trim() : null;
        return !string.IsNullOrEmpty(danceName) && !string.IsNullOrEmpty(url);
    }

    static string SanitizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return string.Empty;
        }

        string name = displayName.Trim().Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        while (name.Contains("  "))
        {
            name = name.Replace("  ", " ");
        }

        name = name.Replace("<", "").Replace(">", "");
        if (name.Length > 48)
        {
            name = name.Substring(0, 48).Trim();
        }

        return name;
    }

    static string SanitizeDanceName(string danceName)
    {
        if (string.IsNullOrWhiteSpace(danceName))
        {
            return string.Empty;
        }

        string name = danceName.Trim().Replace("\\", "/");
        int slash = name.LastIndexOf('/');
        if (slash >= 0)
        {
            name = name.Substring(slash + 1);
        }

        if (name.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - 5);
        }

        char[] invalid = { ':', '*', '?', '"', '<', '>', '|', '\r', '\t' };
        for (int i = 0; i < invalid.Length; i++)
        {
            name = name.Replace(invalid[i].ToString(), "");
        }

        return name.Trim();
    }

    [System.Serializable]
    class UrlDancePayload
    {
        public string name;
        public string url;
    }
}
