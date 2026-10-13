using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns Stop_Dancing into a pause/play toggle and swaps the button sprite.
/// </summary>
public class InGamePausePlayButton : MonoBehaviour
{
    public static InGamePausePlayButton Instance { get; private set; }

    Button button;
    Image image;
    Sprite pauseSprite;
    Sprite playSprite;
    bool showingPauseIcon = true;

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

    void Start()
    {
        BindButton();
        RefreshIcon();
    }

    void LateUpdate()
    {
        RefreshIcon();
    }

    void BindButton()
    {
        if (button != null)
        {
            return;
        }

        GameObject go = GameObject.Find("Stop_Dancing");
        if (go == null)
        {
            return;
        }

        button = go.GetComponent<Button>();
        image = go.GetComponent<Image>();
        if (button == null)
        {
            return;
        }

        pauseSprite = image != null ? image.sprite : null;
        playSprite = FindPlaySprite();
        if (playSprite == null)
        {
            playSprite = pauseSprite;
        }

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(OnClicked);
    }

    Sprite FindPlaySprite()
    {
        GameObject rightArrow = GameObject.Find("Right_Arrow");
        if (rightArrow != null)
        {
            Image rightImage = rightArrow.GetComponent<Image>();
            if (rightImage != null && rightImage.sprite != null && rightImage.sprite != pauseSprite)
            {
                return rightImage.sprite;
            }
        }

        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null && sprite.name == "UI_grey_buttons_light_1_2")
            {
                return sprite;
            }
        }

        return null;
    }

    void OnClicked()
    {
        TogglePlayback();
    }

    public static void TogglePlayback()
    {
        AvatarAnimationPlayer player = FindActiveMainDancer();
        if (player == null)
        {
            return;
        }

        if (player.isPlaying)
        {
            RandomDanceMode.EnsureExists().NotifyManualDanceChange();
            player.PauseDance();
            return;
        }

        if (player.IsPaused)
        {
            player.ResumeDance();
            return;
        }

        AvatarUIItem selected = AvatarUIItem.GetSelectedDance();
        if (selected != null)
        {
            selected.ReplaySelectedDance();
        }
    }

    public static void PausePlayback()
    {
        AvatarAnimationPlayer player = FindActiveMainDancer();
        if (player == null || !player.isPlaying)
        {
            return;
        }

        RandomDanceMode.EnsureExists().NotifyManualDanceChange();
        player.PauseDance();
    }

    public static void ResumePlayback()
    {
        AvatarAnimationPlayer player = FindActiveMainDancer();
        if (player == null)
        {
            return;
        }

        if (player.IsPaused)
        {
            player.ResumeDance();
            return;
        }

        if (!player.isPlaying)
        {
            AvatarUIItem selected = AvatarUIItem.GetSelectedDance();
            if (selected != null)
            {
                selected.ReplaySelectedDance();
            }
        }
    }

    public void RefreshIcon()
    {
        if (button == null)
        {
            BindButton();
        }

        if (image == null)
        {
            return;
        }

        AvatarAnimationPlayer player = FindActiveMainDancer();
        bool playing = player != null && player.isPlaying;
        if (playing == showingPauseIcon && image.sprite == (playing ? pauseSprite : playSprite))
        {
            return;
        }

        showingPauseIcon = playing;
        Sprite next = playing ? pauseSprite : playSprite;
        if (next != null)
        {
            image.sprite = next;
        }
    }

    static AvatarAnimationPlayer FindActiveMainDancer()
    {
        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector != null && selector.transform.childCount > 0)
        {
            int index = Mathf.Clamp(selector.CurrentCharacterIndex, 0, selector.transform.childCount - 1);
            AvatarAnimationPlayer fromSelector = selector.transform.GetChild(index).GetComponent<AvatarAnimationPlayer>();
            if (fromSelector != null)
            {
                return fromSelector;
            }
        }

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

    public static void EnsureExists()
    {
        if (Instance != null || FindFirstObjectByType<InGamePausePlayButton>() != null)
        {
            return;
        }

        GameObject host = GameObject.Find("WebBridge");
        if (host == null)
        {
            host = new GameObject("InGamePausePlayButton");
        }

        if (host.GetComponent<InGamePausePlayButton>() == null)
        {
            host.AddComponent<InGamePausePlayButton>();
        }
    }
}
