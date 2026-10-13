using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public class CharacterSelector : MonoBehaviour
{
    [Header("Setup")]
    public Transform menuContent;   // Drag 'Content' object here
    public Transform previewParent; // Drag 'PreviewBuddy' (parent of previews) here
    
    private int currentCharacterIndex = 0;
    private int builtInCount;
    readonly List<string> displayNames = new List<string>();
    TextMeshProUGUI characterNameLabel;

    void Awake()
    {
        builtInCount = Mathf.Max(0, transform.childCount);
        if (previewParent != null && previewParent.GetComponent<PreviewAvatarStage>() == null)
        {
            previewParent.gameObject.AddComponent<PreviewAvatarStage>();
        }

        PreviewAvatarStage.TryInitializeFrom(previewParent);
        PreviewDanceStickFigure.TryInitializeFrom(previewParent);
    }

    void Start()
    {
        EnsureDisplayNameSlots();
        UpdateCharacterVisibility();
        RefreshMenuBindings();
        RefreshCharacterNameLabel();
    }

    public void NextCharacter()
    {
        StopCurrentDancer();
        AvatarUIItem.ClearHoverPreview();
        currentCharacterIndex = (currentCharacterIndex + 1) % transform.childCount;
        UpdateCharacterVisibility();
        RefreshMenuBindings();
        RefreshCharacterNameLabel();
        AvatarUIItem.RestoreSelectionAfterCharacterSwap();
    }

    public void PreviousCharacter()
    {
        StopCurrentDancer();
        AvatarUIItem.ClearHoverPreview();
        currentCharacterIndex--;
        if (currentCharacterIndex < 0) currentCharacterIndex = transform.childCount - 1;
        UpdateCharacterVisibility();
        RefreshMenuBindings();
        RefreshCharacterNameLabel();
        AvatarUIItem.RestoreSelectionAfterCharacterSwap();
    }

    public int CurrentCharacterIndex => currentCharacterIndex;

    public int BuiltInCount => builtInCount;

    public string GetCharacterNamesJoined()
    {
        EnsureDisplayNameSlots();
        return string.Join("\n", displayNames);
    }

    public bool SelectCharacter(int index)
    {
        if (index < 0 || index >= transform.childCount)
        {
            return false;
        }

        if (index == currentCharacterIndex)
        {
            RefreshCharacterNameLabel();
            return true;
        }

        StopCurrentDancer();
        AvatarUIItem.ClearHoverPreview();
        currentCharacterIndex = index;
        UpdateCharacterVisibility();
        RefreshMenuBindings();
        RefreshCharacterNameLabel();
        AvatarUIItem.RestoreSelectionAfterCharacterSwap();
        return true;
    }

    public bool RemoveCurrentAvatar()
    {
        return RemoveAvatarAt(currentCharacterIndex);
    }

    public bool RemoveAvatarAt(int removeIndex)
    {
        if (removeIndex < builtInCount || removeIndex >= transform.childCount)
        {
            return false;
        }

        if (transform.childCount <= builtInCount)
        {
            return false;
        }

        int nextIndex = removeIndex >= transform.childCount - 1 ? removeIndex - 1 : removeIndex;
        int oldCurrent = currentCharacterIndex;

        if (oldCurrent == removeIndex)
        {
            StopCurrentDancer();
        }
        else
        {
            AvatarAnimationPlayer removedMain = transform.GetChild(removeIndex).GetComponent<AvatarAnimationPlayer>();
            if (removedMain != null)
            {
                removedMain.StopDance();
            }

            if (previewParent != null && removeIndex < previewParent.childCount)
            {
                AvatarAnimationPlayer removedPreview = previewParent.GetChild(removeIndex).GetComponent<AvatarAnimationPlayer>();
                if (removedPreview != null)
                {
                    removedPreview.StopDance();
                }
            }
        }

        AvatarUIItem.ClearHoverPreview();

        Transform main = transform.GetChild(removeIndex);
        main.gameObject.SetActive(false);
        main.SetParent(null, false);
        Destroy(main.gameObject);

        if (previewParent != null && removeIndex < previewParent.childCount)
        {
            Transform preview = previewParent.GetChild(removeIndex);
            preview.gameObject.SetActive(false);
            preview.SetParent(null, false);
            Destroy(preview.gameObject);
        }

        if (removeIndex < displayNames.Count)
        {
            displayNames.RemoveAt(removeIndex);
        }

        if (oldCurrent == removeIndex)
        {
            currentCharacterIndex = Mathf.Clamp(nextIndex, 0, Mathf.Max(0, transform.childCount - 1));
        }
        else if (oldCurrent > removeIndex)
        {
            currentCharacterIndex = oldCurrent - 1;
        }
        else
        {
            currentCharacterIndex = oldCurrent;
        }

        currentCharacterIndex = Mathf.Clamp(currentCharacterIndex, 0, Mathf.Max(0, transform.childCount - 1));
        UpdateCharacterVisibility();
        RefreshMenuBindings();
        RefreshCharacterNameLabel();
        AvatarUIItem.RestoreSelectionAfterCharacterSwap();
        Debug.Log("CharacterSelector: removed avatar at " + removeIndex + ".");
        return true;
    }

    private void StopCurrentDancer()
    {
        // Stop the main character
        var mainDancer = transform.GetChild(currentCharacterIndex).GetComponent<AvatarAnimationPlayer>();
        if (mainDancer != null) mainDancer.StopDance();

        // Stop the preview character
        if (previewParent != null && previewParent.childCount > currentCharacterIndex)
        {
            var previewDancer = previewParent.GetChild(currentCharacterIndex).GetComponent<AvatarAnimationPlayer>();
            if (previewDancer != null) previewDancer.StopDance();
        }
    }

    private void UpdateCharacterVisibility()
    {
        AvatarAnimationPlayer newMainDancer = null;
        AvatarAnimationPlayer newPreviewDancer = null;

        // 1. Activate Main Avatar
        for (int i = 0; i < transform.childCount; i++)
        {
            bool isActive = (i == currentCharacterIndex);
            transform.GetChild(i).gameObject.SetActive(isActive);
            if (isActive) newMainDancer = transform.GetChild(i).GetComponent<AvatarAnimationPlayer>();
        }

        // 2. Activate Preview Avatar
        if (previewParent != null)
        {
            for (int i = 0; i < previewParent.childCount; i++)
            {
                bool isActive = (i == currentCharacterIndex);
                previewParent.GetChild(i).gameObject.SetActive(isActive);
                if (isActive) newPreviewDancer = previewParent.GetChild(i).GetComponent<AvatarAnimationPlayer>();
            }
        }

        RefreshMenuBindings();
        RefreshCharacterNameLabel();
    }

    /// <summary>Call after DanceMenuGenerator builds the menu so each item uses the active avatars.</summary>
    public void RefreshMenuBindings()
    {
        if (menuContent == null)
        {
            return;
        }

        AvatarAnimationPlayer mainDancer = null;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (i == currentCharacterIndex)
            {
                mainDancer = transform.GetChild(i).GetComponent<AvatarAnimationPlayer>();
                break;
            }
        }

        if (mainDancer == null)
        {
            return;
        }

        foreach (Transform box in menuContent)
        {
            AvatarUIItem item = box.GetComponent<AvatarUIItem>();
            if (item != null)
            {
                item.realDancer = mainDancer;
                item.RefreshPreviewImage();
            }
        }

        if (previewParent != null && previewParent.childCount > currentCharacterIndex)
        {
            AvatarAnimationPlayer previewDancer = previewParent.GetChild(currentCharacterIndex).GetComponent<AvatarAnimationPlayer>();
            if (previewDancer != null)
            {
                PreviewDanceStickFigure.AlignToPreviewAvatar(previewDancer.transform);
            }
        }

        Debug.Log("CharacterSelector: menu wired to main '" + mainDancer.gameObject.name + "'.");
    }

    public bool AddExternalAvatar(string displayName, string glbUrl, byte[] glbBytes)
    {
        if (transform.childCount == 0)
        {
            Debug.LogWarning("CharacterSelector.AddExternalAvatar: no template avatar to copy.");
            return false;
        }

        if ((glbBytes == null || glbBytes.Length == 0) && string.IsNullOrWhiteSpace(glbUrl))
        {
            Debug.LogWarning("CharacterSelector.AddExternalAvatar: missing GLB url or bytes.");
            return false;
        }

        StopCurrentDancer();
        AvatarUIItem.ClearHoverPreview();

        int index = transform.childCount;
        string safeName = string.IsNullOrWhiteSpace(displayName) ? ("Avatar_" + index) : displayName.Trim();
        Transform mainTemplate = transform.GetChild(0);
        GameObject main = CreateMainAvatar(mainTemplate, "Avatar_" + index, safeName, glbUrl, glbBytes);

        if (previewParent != null && previewParent.childCount > 0)
        {
            CreatePreviewAvatar(previewParent.GetChild(0), "Preview_" + index, glbUrl, glbBytes);
        }

        currentCharacterIndex = index;
        EnsureDisplayNameSlots();
        displayNames[index] = safeName;
        UpdateCharacterVisibility();
        RefreshCharacterNameLabel();
        AvatarUIItem.RestoreSelectionAfterCharacterSwap();
        Debug.Log("CharacterSelector: added avatar '" + safeName + "'.");
        return main != null;
    }

    public bool RenameCurrentAvatar(string displayName)
    {
        EnsureDisplayNameSlots();
        if (string.IsNullOrWhiteSpace(displayName) || transform.childCount == 0)
        {
            return false;
        }

        if (currentCharacterIndex < 0 || currentCharacterIndex >= displayNames.Count)
        {
            return false;
        }

        displayNames[currentCharacterIndex] = displayName.Trim();
        RefreshCharacterNameLabel();
        return true;
    }

    void EnsureDisplayNameSlots()
    {
        while (displayNames.Count < transform.childCount)
        {
            int index = displayNames.Count;
            displayNames.Add("Character " + (index + 1));
        }
    }

    void RefreshCharacterNameLabel()
    {
        EnsureDisplayNameSlots();
        HideCharacterNameLabel();
    }

    void HideCharacterNameLabel()
    {
        if (characterNameLabel == null)
        {
            GameObject existing = GameObject.Find("Character_Text");
            if (existing != null)
            {
                characterNameLabel = existing.GetComponent<TextMeshProUGUI>();
            }
        }

        if (characterNameLabel != null)
        {
            characterNameLabel.gameObject.SetActive(false);
        }
    }

    GameObject CreateMainAvatar(Transform template, string objectName, string displayName, string glbUrl, byte[] glbBytes)
    {
        GameObject go = new GameObject(objectName);
        go.layer = template.gameObject.layer;
        go.tag = template.gameObject.tag;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = template.localPosition;
        go.transform.localRotation = template.localRotation;
        go.transform.localScale = template.localScale;
        go.SetActive(false);

        ReadyPlayerAvatar srcAvatar = template.GetComponent<ReadyPlayerAvatar>();
        ReadyPlayerAvatar dstAvatar = go.AddComponent<ReadyPlayerAvatar>();
        dstAvatar.CopyStageSettingsFrom(srcAvatar);
        dstAvatar.ConfigureExternalGlb(glbUrl, glbBytes);

        AvatarAnimationPlayer srcPlayer = template.GetComponent<AvatarAnimationPlayer>();
        AvatarAnimationPlayer dstPlayer = go.AddComponent<AvatarAnimationPlayer>();
        dstPlayer.CopyPlaybackSettingsFrom(srcPlayer);

        go.name = objectName;
        return go;
    }

    void CreatePreviewAvatar(Transform template, string objectName, string glbUrl, byte[] glbBytes)
    {
        GameObject go = new GameObject(objectName);
        go.layer = template.gameObject.layer;
        go.tag = template.gameObject.tag;
        go.transform.SetParent(previewParent, false);
        go.SetActive(false);

        RectTransform srcRect = template as RectTransform;
        RectTransform dstRect = go.AddComponent<RectTransform>();
        if (srcRect != null)
        {
            dstRect.anchorMin = srcRect.anchorMin;
            dstRect.anchorMax = srcRect.anchorMax;
            dstRect.pivot = srcRect.pivot;
            dstRect.anchoredPosition = srcRect.anchoredPosition;
            dstRect.sizeDelta = srcRect.sizeDelta;
            dstRect.localRotation = srcRect.localRotation;
            dstRect.localScale = srcRect.localScale;
        }
        else
        {
            go.transform.localPosition = template.localPosition;
            go.transform.localRotation = template.localRotation;
            go.transform.localScale = template.localScale;
        }

        Canvas srcCanvas = template.GetComponent<Canvas>();
        if (srcCanvas != null)
        {
            Canvas dstCanvas = go.AddComponent<Canvas>();
            dstCanvas.renderMode = srcCanvas.renderMode;
            dstCanvas.worldCamera = srcCanvas.worldCamera;
            dstCanvas.overrideSorting = srcCanvas.overrideSorting;
            dstCanvas.sortingOrder = srcCanvas.sortingOrder;
        }

        if (template.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null)
        {
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        LayerFixer srcFixer = template.GetComponent<LayerFixer>();
        LayerFixer dstFixer = go.AddComponent<LayerFixer>();
        if (srcFixer != null)
        {
            dstFixer.targetLayer = srcFixer.targetLayer;
        }

        ReadyPlayerAvatar srcAvatar = template.GetComponent<ReadyPlayerAvatar>();
        ReadyPlayerAvatar dstAvatar = go.AddComponent<ReadyPlayerAvatar>();
        dstAvatar.CopyStageSettingsFrom(srcAvatar);
        dstAvatar.ConfigureExternalGlb(glbUrl, glbBytes);

        AvatarAnimationPlayer srcPlayer = template.GetComponent<AvatarAnimationPlayer>();
        AvatarAnimationPlayer dstPlayer = go.AddComponent<AvatarAnimationPlayer>();
        dstPlayer.CopyPlaybackSettingsFrom(srcPlayer);
    }
}