#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class JuraFontAssetBuilder
{
    const string SourceFontPath = "Assets/Resources/Fonts/Jura-Medium.ttf";
    const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Jura-Medium SDF.asset";
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("UPose/Build Jura Font Asset")]
    public static void Build()
    {
        if (!BuildInternal())
        {
            Debug.LogError("JuraFontAssetBuilder: build failed.");
        }
    }

    public static void BuildFromBatch()
    {
        EditorApplication.Exit(BuildInternal() ? 0 : 1);
    }

    public static bool BuildInternal()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError("JuraFontAssetBuilder: missing source font at " + SourceFontPath);
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
        }

        System.IO.Directory.CreateDirectory("Assets/TextMesh Pro/Resources/Fonts & Materials");

        // Dynamic SDF with a real atlas texture persisted as a sub-asset.
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic);

        if (fontAsset == null)
        {
            Debug.LogError("JuraFontAssetBuilder: TMP_FontAsset.CreateFontAsset failed.");
            return false;
        }

        fontAsset.name = "Jura-Medium SDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        // Keep seeded glyphs in builds (field is internal on TMP_FontAsset).
        SerializedObject so = new SerializedObject(fontAsset);
        SerializedProperty clearProp = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (clearProp != null)
        {
            clearProp.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Seed common Latin glyphs so the atlas is non-empty and readable in editor.
        const string seed =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        fontAsset.TryAddCharacters(seed, out _);

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D atlas = fontAsset.atlasTextures[i];
                if (atlas == null)
                {
                    continue;
                }

                atlas.name = fontAsset.name + " Atlas " + i;
                if (!AssetDatabase.Contains(atlas))
                {
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Jura-Medium SDF Material";
            if (!AssetDatabase.Contains(fontAsset.material))
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            Texture2D mainAtlas = fontAsset.atlasTexture;
            if (mainAtlas != null)
            {
                fontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, mainAtlas);
            }
        }

        EditorUtility.SetDirty(fontAsset);
        TMP_Settings.defaultFontAsset = fontAsset;

        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings != null)
        {
            EditorUtility.SetDirty(settings);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Verify atlas survived serialization.
        TMP_FontAsset reloaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (reloaded == null || reloaded.atlasTexture == null)
        {
            Debug.LogError("JuraFontAssetBuilder: atlas texture still missing after save.");
            return false;
        }

        Debug.Log("JuraFontAssetBuilder: created " + FontAssetPath + " with atlas " + reloaded.atlasTexture.name);
        return true;
    }
}
#endif
