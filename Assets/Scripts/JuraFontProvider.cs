using TMPro;
using UnityEngine;

/// <summary>
/// Loads the Jura Medium TMP font and applies it to UI text.
/// </summary>
public static class JuraFontProvider
{
    const string FontResourcePath = "Fonts/Jura-Medium";
    const string FontAssetResourcePath = "Fonts & Materials/Jura-Medium SDF";

    static TMP_FontAsset cachedFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyToOpenScene()
    {
        ApplyToAllText();
    }

    public static TMP_FontAsset GetFont()
    {
        if (IsUsable(cachedFont))
        {
            return cachedFont;
        }

        cachedFont = Resources.Load<TMP_FontAsset>(FontAssetResourcePath);
        if (IsUsable(cachedFont))
        {
            DanceMenuGenerator.FixTmpFontAsset(cachedFont);
            return cachedFont;
        }

#if UNITY_EDITOR
        cachedFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/Jura-Medium SDF.asset");
        if (IsUsable(cachedFont))
        {
            DanceMenuGenerator.FixTmpFontAsset(cachedFont);
            return cachedFont;
        }
#endif

        Font sourceFont = Resources.Load<Font>(FontResourcePath);
        if (sourceFont == null)
        {
            Debug.LogWarning("JuraFontProvider: could not load font at Resources/" + FontResourcePath);
            return null;
        }

        cachedFont = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (!IsUsable(cachedFont))
        {
            Debug.LogWarning("JuraFontProvider: failed to create TMP font asset from Jura-Medium.");
            cachedFont = null;
            return null;
        }

        cachedFont.name = "Jura-Medium SDF Runtime";
        DanceMenuGenerator.FixTmpFontAsset(cachedFont);
        return cachedFont;
    }

    static bool IsUsable(TMP_FontAsset font)
    {
        if (font == null)
        {
            return false;
        }

        // Broken editor-created assets often have a null atlas entry and throw MissingReferenceException.
        if (font.atlasTexture == null)
        {
            return false;
        }

        Texture2D[] atlases = font.atlasTextures;
        if (atlases == null || atlases.Length == 0 || atlases[0] == null)
        {
            return false;
        }

        return true;
    }

    public static void ApplyToAllText()
    {
        TMP_FontAsset font = GetFont();
        if (font == null)
        {
            return;
        }

        TextMeshProUGUI[] labels = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            DanceMenuGenerator.ApplyMenuLabelFont(labels[i], font);
        }

        TextMeshPro[] worldLabels = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < worldLabels.Length; i++)
        {
            TextMeshPro label = worldLabels[i];
            if (label == null)
            {
                continue;
            }

            DanceMenuGenerator.FixTmpFontAsset(font);
            label.font = font;
            label.UpdateFontAsset();
            label.ForceMeshUpdate(true, true);
        }
    }
}
