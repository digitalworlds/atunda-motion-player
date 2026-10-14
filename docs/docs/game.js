(() => {
  const canvas = document.querySelector("#unity-canvas");
  const container = document.querySelector("#unity-container");
  const btnPrevMove = document.querySelector("#btn-prev-move");
  const btnNextMove = document.querySelector("#btn-next-move");
  const btnRandomMode = document.querySelector("#btn-random-mode");
  const btnPrevCharacter = document.querySelector("#btn-prev-character");
  const btnNextCharacter = document.querySelector("#btn-next-character");
  const btnFullscreen = document.querySelector("#btn-fullscreen");
  const btnToggleDanceList = document.querySelector("#btn-toggle-dance-list");
  const btnExitFullscreen = document.querySelector("#btn-exit-fullscreen");
  const danceNameInput = document.querySelector("#dance-name");
  const danceUrlInput = document.querySelector("#dance-url");
  const danceFileInput = document.querySelector("#dance-file");
  const btnLoadDance = document.querySelector("#btn-load-dance");
  const btnRenameDance = document.querySelector("#btn-rename-dance");
  const btnRemoveDance = document.querySelector("#btn-remove-dance");
  const loadDanceForm = document.querySelector("#form-load-dance");
  const loadDanceStatus = document.querySelector("#load-dance-status");
  const avatarNameInput = document.querySelector("#avatar-name");
  const avatarUrlInput = document.querySelector("#avatar-url");
  const avatarFileInput = document.querySelector("#avatar-file");
  const btnLoadAvatar = document.querySelector("#btn-load-avatar");
  const btnRenameAvatar = document.querySelector("#btn-rename-avatar");
  const btnRemoveAvatar = document.querySelector("#btn-remove-avatar");
  const loadAvatarForm = document.querySelector("#form-load-avatar");
  const loadAvatarStatus = document.querySelector("#load-avatar-status");
  const characterListRoot = document.querySelector("#character-list");
  const characterListItems = document.querySelector("#character-list-items");

  const controlButtons = [
    btnPrevMove,
    btnNextMove,
    btnRandomMode,
    btnPrevCharacter,
    btnNextCharacter,
    btnFullscreen,
    btnToggleDanceList,
    btnLoadDance,
    btnRenameDance,
    btnRemoveDance,
    btnLoadAvatar,
    btnRenameAvatar,
    btnRemoveAvatar,
  ];

  let unityInstance = null;
  let randomModeOn = false;
  let danceListVisible = true;
  let characters = [];
  let selectedCharacterIndex = 0;
  let loadedDanceFileKey = null;
  let loadedAvatarFileKey = null;
  let danceLoadInFlight = false;
  let avatarLoadInFlight = false;

  // WebGL mouse-wheel deltas are much larger than in the Unity editor.
  const unityWheelScale = 0.28;
  if (canvas) {
    let forwardingWheel = false;
    canvas.addEventListener(
      "wheel",
      (event) => {
        if (forwardingWheel) {
          return;
        }

        event.preventDefault();
        event.stopImmediatePropagation();
        forwardingWheel = true;
        try {
          canvas.dispatchEvent(
            new WheelEvent("wheel", {
              bubbles: true,
              cancelable: true,
              composed: true,
              clientX: event.clientX,
              clientY: event.clientY,
              screenX: event.screenX,
              screenY: event.screenY,
              deltaX: event.deltaX * unityWheelScale,
              deltaY: event.deltaY * unityWheelScale,
              deltaZ: event.deltaZ * unityWheelScale,
              deltaMode: event.deltaMode,
              ctrlKey: event.ctrlKey,
              shiftKey: event.shiftKey,
              altKey: event.altKey,
              metaKey: event.metaKey,
            })
          );
        } finally {
          forwardingWheel = false;
        }
      },
      { capture: true, passive: false }
    );
  }

  const siteTextFields = [
    danceNameInput,
    danceUrlInput,
    avatarNameInput,
    avatarUrlInput,
  ].filter(Boolean);
  const siteTextFieldSet = new Set(siteTextFields);
  let typingField = null;

  function releaseUnityKeyboard() {
    if (canvas) {
      canvas.tabIndex = -1;
      if (typeof canvas.blur === "function") {
        canvas.blur();
      }
    }
  }

  function beginTyping(field) {
    typingField = field;
    releaseUnityKeyboard();
    try {
      field.focus();
    } catch (_) {
      /* ignore */
    }
  }

  function endTyping() {
    typingField = null;
  }

  function fieldRange(field) {
    const len = field.value.length;
    let start = field.selectionStart;
    let end = field.selectionEnd;
    if (typeof start !== "number") start = len;
    if (typeof end !== "number") end = len;
    return { start, end, len };
  }

  function setFieldValue(field, value, caret) {
    field.value = value;
    try {
      field.setSelectionRange(caret, caret);
    } catch (_) {
      /* some input types reject caret APIs */
    }
    field.dispatchEvent(new Event("input", { bubbles: true }));
  }

  function insertIntoField(field, text) {
    const { start, end } = fieldRange(field);
    setFieldValue(field, field.value.slice(0, start) + text + field.value.slice(end), start + text.length);
  }

  function handleSiteFieldKeydown(event) {
    const field = typingField && siteTextFieldSet.has(typingField) ? typingField : null;
    if (!field) {
      return;
    }

    event.stopImmediatePropagation();
    event.preventDefault();
    beginTyping(field);

    if (event.isComposing) {
      return;
    }

    if (event.ctrlKey || event.metaKey) {
      if (event.key === "a" || event.key === "A") {
        try {
          field.setSelectionRange(0, field.value.length);
        } catch (_) {
          /* ignore */
        }
      } else if (event.key === "v" || event.key === "V") {
        if (navigator.clipboard && navigator.clipboard.readText) {
          navigator.clipboard.readText().then((text) => insertIntoField(field, text)).catch(() => {});
        }
      }
      return;
    }

    const { start, end, len } = fieldRange(field);
    const value = field.value;

    if (event.key === "Backspace") {
      if (start !== end) {
        setFieldValue(field, value.slice(0, start) + value.slice(end), start);
      } else if (start > 0) {
        setFieldValue(field, value.slice(0, start - 1) + value.slice(end), start - 1);
      }
      return;
    }

    if (event.key === "Delete") {
      if (start !== end) {
        setFieldValue(field, value.slice(0, start) + value.slice(end), start);
      } else if (start < len) {
        setFieldValue(field, value.slice(0, start) + value.slice(end + 1), start);
      }
      return;
    }

    if (event.key === "Enter") {
      if (field === danceNameInput || field === danceUrlInput) {
        loadDanceFromInputs();
      } else if (field === avatarNameInput || field === avatarUrlInput) {
        loadAvatarFromInputs();
      }
      return;
    }

    if (event.key.length === 1) {
      insertIntoField(field, event.key);
    }
  }

  window.addEventListener("keydown", handleSiteFieldKeydown, true);

  siteTextFields.forEach((field) => {
    field.addEventListener("pointerdown", (event) => {
      event.stopPropagation();
      beginTyping(field);
    });
    field.addEventListener("click", () => beginTyping(field));
    field.addEventListener("focus", () => beginTyping(field));
    field.addEventListener("blur", () => {
      if (typingField === field) {
        window.setTimeout(() => {
          if (document.activeElement !== field) {
            endTyping();
          }
        }, 0);
      }
    });
    field.addEventListener("paste", (event) => {
      event.preventDefault();
      event.stopPropagation();
      const text = (event.clipboardData || window.clipboardData).getData("text");
      insertIntoField(field, text);
    });
  });

  if (canvas) {
    canvas.addEventListener("pointerdown", () => endTyping());
  }

  function setControlsEnabled(enabled) {
    controlButtons.forEach((btn) => {
      if (btn) btn.disabled = !enabled;
    });
  }

  function callUnity(methodName, value) {
    if (!unityInstance) {
      return false;
    }

    // GameObject name must match the object in your Unity scene.
    const gameObject = "WebBridge";

    try {
      if (typeof value === "undefined") {
        unityInstance.SendMessage(gameObject, methodName);
      } else {
        unityInstance.SendMessage(gameObject, methodName, value);
      }
      return true;
    } catch (err) {
      console.error(err);
      return false;
    }
  }

  // CharacterSelector lives on this GameObject (same as in-game arrows).
  const characterSelectorObject = "Characters (1)";
  let cachedDanceCount = null;

  async function getDanceCount() {
    if (cachedDanceCount != null) {
      return cachedDanceCount;
    }

    try {
      const response = await fetch("StreamingAssets/atunda/dances.manifest.json", {
        cache: "no-cache",
      });
      const dances = await response.json();
      cachedDanceCount = Array.isArray(dances) ? dances.length : 0;
    } catch (err) {
      console.error("Failed to load dance list:", err);
      cachedDanceCount = 0;
    }

    return cachedDanceCount;
  }

  function nextCharacter() {
    callUnity("NextCharacter");
    if (characters.length > 0) {
      selectedCharacterIndex = (selectedCharacterIndex + 1) % characters.length;
      renderCharacterList();
    }
  }

  function previousCharacter() {
    if (!callUnity("PreviousCharacter") && unityInstance) {
      try {
        unityInstance.SendMessage(characterSelectorObject, "PreviousCharacter");
      } catch (err) {
        console.error(err);
      }
    }
    if (characters.length > 0) {
      selectedCharacterIndex =
        (selectedCharacterIndex - 1 + characters.length) % characters.length;
      renderCharacterList();
    }
  }

  function nextDanceMove() {
    setRandomModeUi(false);
    callUnity("NextDanceMove");
  }

  function toggleRandomDanceMode() {
    callUnity("ToggleRandomDanceMode");
    setRandomModeUi(false);
  }

  function updateDanceListButton() {
    if (!btnToggleDanceList) return;
    btnToggleDanceList.textContent = danceListVisible ? "Hide dance list" : "Show dance list";
  }

  function setDanceListVisible(visible) {
    danceListVisible = !!visible;
    callUnity("SetDanceListVisible", danceListVisible ? "1" : "0");
    updateDanceListButton();
  }

  function toggleDanceList() {
    setDanceListVisible(!danceListVisible);
  }

  function setLoadDanceStatus(message) {
    if (loadDanceStatus) {
      loadDanceStatus.textContent = message;
    }
  }

  function fileKey(file) {
    if (!file) {
      return null;
    }
    return `${file.name}:${file.size}:${file.lastModified}`;
  }

  async function loadDanceFromInputs() {
    const typedName = (danceNameInput && danceNameInput.value) || "";
    const danceUrl = (danceUrlInput && danceUrlInput.value) || "";
    const file = danceFileInput && danceFileInput.files && danceFileInput.files[0];
    const danceName = typedName.trim() || (file && file.name.replace(/\.json$/i, "")) || "";

    setRandomModeUi(false);

    if (!file && !danceUrl.trim()) {
      renameCurrentDance();
      return;
    }

    if (file) {
      const name = danceName || "CustomDance";
      const key = fileKey(file);
      if (key && key === loadedDanceFileKey) {
        renameCurrentDance();
        return;
      }
      if (!unityInstance) {
        setLoadDanceStatus("Wait until the game finishes loading, then choose the file again.");
        return;
      }
      if (danceLoadInFlight) {
        return;
      }

      danceLoadInFlight = true;
      setLoadDanceStatus(`Loading ${name} from file…`);
      try {
        const head = await file.slice(0, 65536).text();
        if (!head.includes("npz-json")) {
          setLoadDanceStatus(
            "That JSON is not an Atunda dance file. Use a dance export with \"npz-json\" in it (the same format as the built-in list)."
          );
          return;
        }

        window._pendingDanceJson = await file.arrayBuffer();
        if (!callUnity("LoadDanceFromPendingFile", name)) {
          setLoadDanceStatus("The game could not receive that file.");
          return;
        }
        loadedDanceFileKey = key;
        if (cachedDanceCount != null) {
          cachedDanceCount += 1;
        }
        setLoadDanceStatus(`Loaded ${name}.`);
      } catch (err) {
        console.error(err);
        setLoadDanceStatus("Could not read that JSON file.");
      } finally {
        danceLoadInFlight = false;
      }
      return;
    }

    if (!danceName || !danceUrl.trim()) {
      setLoadDanceStatus("Enter a dance name and a JSON URL, or choose a .json file.");
      return;
    }

    setLoadDanceStatus(`Loading ${danceName}…`);
    callUnity(
      "LoadDanceFromUrl",
      JSON.stringify({
        name: danceName,
        url: danceUrl.trim(),
      })
    );
    if (cachedDanceCount != null) {
      cachedDanceCount += 1;
    }
  }

  function loadDanceFromUrl(name, url) {
    if (danceNameInput && name) danceNameInput.value = name;
    if (danceUrlInput && url) danceUrlInput.value = url;
    loadDanceFromInputs();
  }

  function renameCurrentDance() {
    const name = ((danceNameInput && danceNameInput.value) || "").trim();
    if (!name) {
      setLoadDanceStatus("Type a dance name, then click Rename dance.");
      return;
    }

    callUnity("RenameCurrentDance", name);
    setLoadDanceStatus(`Renamed current dance to ${name}.`);
  }

  function removeCurrentDance() {
    if (!unityInstance) {
      setLoadDanceStatus("Wait until the game finishes loading, then click Remove dance.");
      return;
    }

    setRandomModeUi(false);
    if (!callUnity("RemoveCurrentDance", "")) {
      setLoadDanceStatus("The game could not remove that dance.");
      return;
    }

    if (cachedDanceCount != null && cachedDanceCount > 1) {
      cachedDanceCount -= 1;
    }

    if (danceNameInput) {
      danceNameInput.value = "";
    }
    if (danceFileInput) {
      danceFileInput.value = "";
    }
    loadedDanceFileKey = null;

    setLoadDanceStatus("Removed the selected dance. Refresh the page to restore built-in dances.");
  }

  function setLoadAvatarStatus(message) {
    if (loadAvatarStatus) {
      loadAvatarStatus.textContent = message;
    }
  }

  async function loadAvatarFromInputs() {
    const avatarName = (avatarNameInput && avatarNameInput.value) || "";
    const avatarUrl = (avatarUrlInput && avatarUrlInput.value) || "";
    const file = avatarFileInput && avatarFileInput.files && avatarFileInput.files[0];
    const name = avatarName.trim() || (file && file.name.replace(/\.glb$/i, "")) || "CustomAvatar";

    if (!file && !avatarUrl.trim()) {
      renameCurrentAvatar();
      return;
    }

    if (file) {
      const key = fileKey(file);
      if (key && key === loadedAvatarFileKey) {
        renameCurrentAvatar();
        return;
      }
      if (!unityInstance) {
        setLoadAvatarStatus("Wait until the game finishes loading, then choose the file again.");
        return;
      }
      if (avatarLoadInFlight) {
        return;
      }

      avatarLoadInFlight = true;
      setLoadAvatarStatus(`Loading ${name} from file…`);
      try {
        const magicBuf = await file.slice(0, 4).arrayBuffer();
        const magic = new TextDecoder().decode(new Uint8Array(magicBuf));
        if (magic !== "glTF") {
          setLoadAvatarStatus("That file is not a GLB avatar. Choose a .glb file.");
          return;
        }

        window._pendingAvatarGlb = await file.arrayBuffer();
        if (!callUnity("LoadAvatarFromPendingFile", name)) {
          setLoadAvatarStatus("The game could not receive that file.");
          return;
        }
        loadedAvatarFileKey = key;
        rememberAddedCharacter(name);
        setLoadAvatarStatus(`Loaded ${name}.`);
      } catch (err) {
        console.error(err);
        setLoadAvatarStatus("Could not read that GLB file.");
      } finally {
        avatarLoadInFlight = false;
      }
      return;
    }

    if (!avatarUrl.trim()) {
      setLoadAvatarStatus("Enter a GLB URL or choose a .glb file.");
      return;
    }

    setLoadAvatarStatus(`Loading ${name}…`);
    callUnity(
      "LoadAvatarFromUrl",
      JSON.stringify({
        name,
        url: avatarUrl.trim(),
      })
    );
    rememberAddedCharacter(name);
  }

  function rememberAddedCharacter(name) {
    const label = (name || "CustomAvatar").trim() || "CustomAvatar";
    characters.push({ name: label });
    selectedCharacterIndex = characters.length - 1;
    renderCharacterList();
  }

  function setCharacterList(namesText, selectedIndex) {
    const names = String(namesText || "")
      .split("\n")
      .map((name) => name.trim())
      .filter(Boolean);
    if (names.length === 0) {
      return;
    }

    characters = names.map((name) => ({ name }));
    selectedCharacterIndex = Math.max(
      0,
      Math.min(Number(selectedIndex) || 0, characters.length - 1)
    );
    renderCharacterList();
    if (avatarNameInput && characters[selectedCharacterIndex]) {
      avatarNameInput.value = characters[selectedCharacterIndex].name;
    }
  }

  window.atundaSetCharacters = setCharacterList;

  function renderCharacterList() {
    if (!characterListItems || !characterListRoot) {
      return;
    }

    characterListItems.innerHTML = "";
    if (characters.length === 0) {
      characterListRoot.hidden = true;
      return;
    }

    characterListRoot.hidden = false;
    characters.forEach((character, index) => {
      const button = document.createElement("button");
      button.type = "button";
      button.className =
        "character-list-item" + (index === selectedCharacterIndex ? " is-selected" : "");
      button.textContent = character.name;
      button.addEventListener("click", () => selectCharacter(index));
      characterListItems.appendChild(button);
    });
  }

  function selectCharacter(index) {
    if (index < 0 || index >= characters.length) {
      return;
    }

    selectedCharacterIndex = index;
    renderCharacterList();
    if (avatarNameInput) {
      avatarNameInput.value = characters[index].name;
    }
    callUnity("SelectCharacter", String(index));
  }

  function removeCurrentAvatar() {
    if (!unityInstance) {
      setLoadAvatarStatus("Wait until the game finishes loading, then click Remove character.");
      return;
    }

    if (characters.length <= 1) {
      setLoadAvatarStatus("Keep at least one character.");
      return;
    }

    if (!callUnity("RemoveCurrentAvatar", "")) {
      setLoadAvatarStatus("The game could not remove that character.");
      return;
    }

    characters.splice(selectedCharacterIndex, 1);
    selectedCharacterIndex = Math.min(selectedCharacterIndex, characters.length - 1);
    renderCharacterList();
    if (avatarNameInput) {
      avatarNameInput.value = characters[selectedCharacterIndex]
        ? characters[selectedCharacterIndex].name
        : "";
    }
    if (avatarFileInput) {
      avatarFileInput.value = "";
    }
    loadedAvatarFileKey = null;
    setLoadAvatarStatus(
      "Removed the selected character. Refresh the page to restore original characters."
    );
  }

  function renameCurrentAvatar() {
    const name = ((avatarNameInput && avatarNameInput.value) || "").trim();
    if (!name) {
      setLoadAvatarStatus("Type a character name, then click Rename character.");
      return;
    }

    callUnity("RenameCurrentAvatar", name);
    if (characters[selectedCharacterIndex]) {
      characters[selectedCharacterIndex].name = name;
      renderCharacterList();
    }
    setLoadAvatarStatus(`Renamed current character to ${name}.`);
  }

  function setRandomModeUi(on) {
    randomModeOn = !!on;
    if (!btnRandomMode) return;
    btnRandomMode.classList.toggle("is-active", randomModeOn);
    btnRandomMode.textContent = "Random";
  }

  // Current WebGL build has NextDanceMove but not PreviousDanceMove.
  // Calling next (count - 1) times wraps to the previous dance.
  async function previousDanceMove() {
    if (!unityInstance) return;
    setRandomModeUi(false);

    const count = await getDanceCount();
    if (count <= 0) {
      callUnity("PreviousDanceMove");
      return;
    }

    if (count === 1) {
      callUnity("NextDanceMove");
      return;
    }

    for (let i = 0; i < count - 1; i++) {
      callUnity("NextDanceMove");
    }
  }

  function getFullscreenElement() {
    return (
      document.fullscreenElement ||
      document.webkitFullscreenElement ||
      document.msFullscreenElement ||
      null
    );
  }

  function isImmersive() {
    return document.body.classList.contains("immersive");
  }

  function updateFullscreenButton() {
    const on = !!getFullscreenElement() || isImmersive();
    if (btnFullscreen) {
      btnFullscreen.textContent = on ? "Exit fullscreen" : "Fullscreen";
    }
    if (btnExitFullscreen) {
      btnExitFullscreen.hidden = !on;
    }
  }

  async function requestDomFullscreen(el) {
    if (el.requestFullscreen) {
      await el.requestFullscreen();
      return true;
    }
    if (el.webkitRequestFullscreen) {
      el.webkitRequestFullscreen();
      return true;
    }
    if (el.webkitRequestFullScreen) {
      el.webkitRequestFullScreen();
      return true;
    }
    if (el.msRequestFullscreen) {
      el.msRequestFullscreen();
      return true;
    }
    return false;
  }

  async function exitDomFullscreen() {
    if (document.exitFullscreen) {
      await document.exitFullscreen();
      return;
    }
    if (document.webkitExitFullscreen) {
      document.webkitExitFullscreen();
      return;
    }
    if (document.webkitCancelFullScreen) {
      document.webkitCancelFullScreen();
      return;
    }
    if (document.msExitFullscreen) {
      document.msExitFullscreen();
    }
  }

  // Unity SetFullscreen often fails on mobile. Prefer browser Fullscreen API,
  // with a CSS immersive fallback for iOS Safari.
  async function toggleFullscreen() {
    const target = container || canvas;

    if (getFullscreenElement()) {
      await exitDomFullscreen();
      document.body.classList.remove("immersive");
      updateFullscreenButton();
      return;
    }

    if (isImmersive()) {
      document.body.classList.remove("immersive");
      updateFullscreenButton();
      return;
    }

    let entered = false;
    try {
      entered = await requestDomFullscreen(target);
    } catch (err) {
      console.warn("Browser fullscreen failed:", err);
      entered = false;
    }

    if (!entered) {
      // iOS / restricted mobile browsers
      document.body.classList.add("immersive");
      try {
        if (screen.orientation && screen.orientation.lock) {
          await screen.orientation.lock("landscape").catch(() => {});
        }
      } catch (_) {
        /* ignore */
      }
    } else if (unityInstance && typeof unityInstance.SetFullscreen === "function") {
      try {
        unityInstance.SetFullscreen(1);
      } catch (_) {
        /* ignore — DOM fullscreen already applied */
      }
    }

    updateFullscreenButton();
  }

  function unityShowBanner(msg, type) {
    const warningBanner = document.querySelector("#unity-warning");

    function updateBannerVisibility() {
      warningBanner.style.display = warningBanner.children.length ? "block" : "none";
    }

    const div = document.createElement("div");
    div.innerHTML = msg;
    warningBanner.appendChild(div);

    if (type === "error") {
      div.style.cssText = "background: red; padding: 10px; color: #fff;";
    } else {
      if (type === "warning") {
        div.style.cssText = "background: yellow; padding: 10px; color: #000;";
      }
      setTimeout(() => {
        warningBanner.removeChild(div);
        updateBannerVisibility();
      }, 5000);
    }

    updateBannerVisibility();
  }

  btnPrevMove.addEventListener("click", () => {
    setRandomModeUi(false);
    previousDanceMove();
  });
  btnNextMove.addEventListener("click", () => nextDanceMove());
  if (btnRandomMode) {
    btnRandomMode.addEventListener("click", () => toggleRandomDanceMode());
  }
  btnPrevCharacter.addEventListener("click", () => previousCharacter());
  btnNextCharacter.addEventListener("click", () => nextCharacter());
  btnFullscreen.addEventListener("click", () => {
    toggleFullscreen();
  });
  if (btnToggleDanceList) {
    btnToggleDanceList.addEventListener("click", () => toggleDanceList());
  }
  if (btnExitFullscreen) {
    btnExitFullscreen.addEventListener("click", (event) => {
      event.stopPropagation();
      toggleFullscreen();
    });
  }

  if (loadDanceForm) {
    loadDanceForm.addEventListener("submit", (event) => {
      event.preventDefault();
      loadDanceFromInputs();
    });
  }

  if (loadAvatarForm) {
    loadAvatarForm.addEventListener("submit", (event) => {
      event.preventDefault();
      loadAvatarFromInputs();
    });
  }

  if (btnRenameDance) {
    btnRenameDance.addEventListener("click", () => renameCurrentDance());
  }

  if (btnRemoveDance) {
    btnRemoveDance.addEventListener("click", () => removeCurrentDance());
  }

  if (btnRenameAvatar) {
    btnRenameAvatar.addEventListener("click", () => renameCurrentAvatar());
  }

  if (btnRemoveAvatar) {
    btnRemoveAvatar.addEventListener("click", () => removeCurrentAvatar());
  }

  function afterFileChosen(fileInput, nameInput, extRe, setStatus, kind, loadFn, autoLoad) {
    const file = fileInput && fileInput.files && fileInput.files[0];
    if (!file) {
      return;
    }

    if (nameInput) {
      nameInput.disabled = false;
      nameInput.readOnly = false;
      if (!nameInput.value.trim()) {
        nameInput.value = file.name.replace(extRe, "");
      }
      releaseUnityKeyboard();
      nameInput.focus();
    }

    if (autoLoad) {
      setStatus(`${file.name} selected. Loading ${kind}…`);
      loadFn();
      return;
    }

    setStatus(`${file.name} selected. Click Load ${kind} to load it.`);
  }

  if (danceFileInput) {
    danceFileInput.addEventListener("change", () => {
      afterFileChosen(
        danceFileInput,
        danceNameInput,
        /\.json$/i,
        setLoadDanceStatus,
        "dance",
        loadDanceFromInputs,
        false
      );
    });
  }

  if (avatarFileInput) {
    avatarFileInput.addEventListener("change", () => {
      afterFileChosen(
        avatarFileInput,
        avatarNameInput,
        /\.glb$/i,
        setLoadAvatarStatus,
        "character",
        loadAvatarFromInputs,
        false
      );
    });
  }

  document.addEventListener("fullscreenchange", updateFullscreenButton);
  document.addEventListener("webkitfullscreenchange", updateFullscreenButton);

  // Public JS API for embedding / console use
  window.atundaApi = {
    nextDanceMove,
    previousDanceMove,
    nextCharacter,
    previousCharacter,
    toggleRandomDanceMode,
    toggleFullscreen,
    loadDanceFromUrl,
    loadDanceFromInputs,
    loadAvatarFromInputs,
    renameCurrentDance,
    removeCurrentDance,
    renameCurrentAvatar,
    removeCurrentAvatar,
    selectCharacter,
    setCharacterList,
    setDanceListVisible,
    toggleDanceList,
  };

  const buildUrl = "Build";
  const cacheBust = "20260815-characters";
  const loaderUrl = `${buildUrl}/UPOSE-BACKUP.loader.js?v=${cacheBust}`;
  const config = {
    arguments: [],
    dataUrl: `${buildUrl}/UPOSE-BACKUP.data?v=${cacheBust}`,
    frameworkUrl: `${buildUrl}/UPOSE-BACKUP.framework.js?v=${cacheBust}`,
    codeUrl: `${buildUrl}/UPOSE-BACKUP.wasm?v=${cacheBust}`,
    streamingAssetsUrl: "StreamingAssets",
    companyName: "DefaultCompany",
    productName: "UPose",
    productVersion: "0.1.0",
    showBanner: unityShowBanner,
  };

  if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
    const meta = document.createElement("meta");
    meta.name = "viewport";
    meta.content =
      "width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes";
    document.head.appendChild(meta);
    document.querySelector("#unity-container").className = "unity-mobile";
    canvas.className = "unity-mobile";
  }

  document.querySelector("#unity-loading-bar").style.display = "flex";

  const script = document.createElement("script");
  script.src = loaderUrl;
  script.onload = () => {
    createUnityInstance(canvas, config, (progress) => {
      document.querySelector("#unity-progress-bar-full").style.width = `${100 * progress}%`;
    })
      .then((instance) => {
        unityInstance = instance;
        window.unityInstance = instance;
        document.querySelector("#unity-loading-bar").style.display = "none";
        setControlsEnabled(true);
        updateFullscreenButton();
        releaseUnityKeyboard();
        callUnity("ReportCharacters", "");
      })
      .catch((message) => {
        console.error(message);
        setControlsEnabled(false);
        alert(message);
      });
  };
  script.onerror = () => {
    alert("Could not load Unity loader. Serve this folder over http://localhost (not file://).");
  };

  document.body.appendChild(script);

  // Stronger press feedback on touch / mouse for website buttons.
  document.querySelectorAll(".btn, .exit-fullscreen-btn").forEach((btn) => {
    const press = () => {
      if (!btn.disabled) btn.classList.add("is-pressed");
    };
    const release = () => btn.classList.remove("is-pressed");
    btn.addEventListener("pointerdown", press);
    btn.addEventListener("pointerup", release);
    btn.addEventListener("pointerleave", release);
    btn.addEventListener("pointercancel", release);
  });
})();
