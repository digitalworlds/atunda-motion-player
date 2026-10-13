using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates an in-game "Random" button centered between Left_Arrow and Right_Arrow.
/// </summary>
public class InGameRandomDanceButton : MonoBehaviour
{
    [SerializeField] Vector2 size = new Vector2(280f, 80f);
    // Match DanceMenuGenerator scroll panel outline.
    [SerializeField] float borderWidth = 8f;
    [SerializeField] int cornerRadius = 14;
    [SerializeField] Color borderColor = new Color(78f / 255f, 84f / 255f, 118f / 255f, 1f);
    [SerializeField] Color fillColor = new Color(104f / 255f, 111f / 255f, 153f / 255f, 1f);

    Button button;
    RectTransform buttonRect;
    TextMeshProUGUI label;
    RectTransform leftArrow;
    RectTransform rightArrow;

    void Start()
    {
        StartCoroutine(SetupWhenReady());
    }

    IEnumerator SetupWhenReady()
    {
        // Wait a frame so Canvas / arrow layout is ready.
        yield return null;
        EnsureButton();
        PositionBetweenArrows();
        ApplyDanceListVisibility();
        // One more frame in case layout rebuilds.
        yield return null;
        PositionBetweenArrows();
        ApplyDanceListVisibility();
    }

    void LateUpdate()
    {
        if (buttonRect != null && leftArrow != null && rightArrow != null)
        {
            PositionBetweenArrows();
        }
    }

    void EnsureButton()
    {
        if (button != null)
        {
            return;
        }

        leftArrow = FindArrow("Left_Arrow");
        rightArrow = FindArrow("Right_Arrow");

        Transform parent = null;
        if (leftArrow != null && rightArrow != null)
        {
            parent = leftArrow.parent;
        }
        else
        {
            Canvas canvas = FindPreferredCanvas();
            if (canvas != null)
            {
                parent = canvas.transform;
            }
        }

        if (parent == null)
        {
            return;
        }

        GameObject go = new GameObject("RandomDanceButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        buttonRect = go.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = size;
        buttonRect.localScale = Vector3.one;

        int radiusPx = Mathf.Clamp(cornerRadius, 6, 28);
        float border = Mathf.Max(1.5f, borderWidth);

        // Outer rounded rect = outline (same approach as scroll panel / dance cards).
        Image borderImage = go.GetComponent<Image>();
        DanceMenuGenerator.ApplyRoundedSprite(borderImage, borderColor, radiusPx);
        borderImage.raycastTarget = true;

        GameObject fillGo = new GameObject("ButtonFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(go.transform, false);
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(border, border);
        fillRt.offsetMax = new Vector2(-border, -border);
        fillRt.localScale = Vector3.one;

        Image fillImage = fillGo.GetComponent<Image>();
        int innerRadius = Mathf.Max(4, radiusPx - Mathf.RoundToInt(border));
        DanceMenuGenerator.ApplyRoundedSprite(fillImage, fillColor, innerRadius);
        fillImage.raycastTarget = false;

        button = go.GetComponent<Button>();
        button.targetGraphic = borderImage;
        button.onClick.AddListener(OnClicked);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        label = textGo.GetComponent<TextMeshProUGUI>();
        DanceMenuGenerator.ApplyMenuLabelFont(label, null);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 34;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = "Random";

        // Keep button above the arrows in draw order.
        go.transform.SetAsLastSibling();
        ApplyDanceListVisibility();
    }

    void ApplyDanceListVisibility()
    {
        if (button == null)
        {
            return;
        }

        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = button.gameObject.AddComponent<CanvasGroup>();
        }

        bool visible = WebBridge.DanceListUiVisible;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    void PositionBetweenArrows()
    {
        if (buttonRect == null)
        {
            return;
        }

        if (leftArrow == null)
        {
            leftArrow = FindArrow("Left_Arrow");
        }

        if (rightArrow == null)
        {
            rightArrow = FindArrow("Right_Arrow");
        }

        if (leftArrow == null || rightArrow == null)
        {
            return;
        }

        if (buttonRect.parent != leftArrow.parent)
        {
            buttonRect.SetParent(leftArrow.parent, false);
        }

        // Use visual rect centers (pivots are bottom-right, so transform.position is biased).
        Vector3 mid = (GetWorldCenter(leftArrow) + GetWorldCenter(rightArrow)) * 0.5f;
        buttonRect.position = mid;
    }

    static Vector3 GetWorldCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    static RectTransform FindArrow(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    Canvas FindPreferredCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Canvas best = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isActiveAndEnabled)
            {
                continue;
            }

            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }

            if (best == null)
            {
                best = c;
            }
        }

        return best;
    }

    void OnClicked()
    {
        RandomDanceMode.EnsureExists().PlayRandomNow();
    }

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<InGameRandomDanceButton>() != null)
        {
            return;
        }

        GameObject host = GameObject.Find("WebBridge");
        if (host == null)
        {
            host = new GameObject("InGameRandomDanceButton");
        }

        if (host.GetComponent<InGameRandomDanceButton>() == null)
        {
            host.AddComponent<InGameRandomDanceButton>();
        }
    }
}
