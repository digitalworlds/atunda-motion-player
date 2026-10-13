# UPose viewer — JavaScript API

Use this WebGL build as a 3D dance viewer on any website. The page talks to Unity through one object, `WebBridge`. Your HTML buttons call the same actions as the in-game overlay (left/right, random, pause/play, dance list, and more).

## How the bridge works

Unity WebGL exposes `unityInstance.SendMessage(gameObjectName, methodName, optionalString)`.

Every public method on `Assets/WebBridge.cs` is a C# `public void` on a scene object named `WebBridge`. A page button is just:

```js
unityInstance.SendMessage("WebBridge", "MethodName");
unityInstance.SendMessage("WebBridge", "MethodName", "argument");
```

That call is forwarded into Unity and runs the matching C# method. Methods with no C# argument can omit the third parameter. Methods that take a `string` **must** get a string (use `""` if the C# parameter is unused).

Unity does **not** capture keyboard input (`WebGLInput.captureAllKeyboardInput = false`), so the host page can bind its own shortcuts.

Wait until `createUnityInstance(...)` resolves before sending messages.

```js
let unityInstance = null;

createUnityInstance(canvas, config, onProgress).then((instance) => {
  unityInstance = instance;
  unityInstance.SendMessage("WebBridge", "HideGameUi");
});

function send(method, arg) {
  if (!unityInstance) return;
  if (arg === undefined) unityInstance.SendMessage("WebBridge", method);
  else unityInstance.SendMessage("WebBridge", method, String(arg));
}
```

## Embedding the build

Host the WebGL output (`index.html`, `Build/`, `StreamingAssets/`, `TemplateData/`) on the same origin as your site, or configure CORS if you load the `.data` / `.wasm` from another host.

Typical loader (same pattern as `docs/index.html`):

```html
<canvas id="unity-canvas" width="960" height="600" tabindex="-1"></canvas>
<script>
  const canvas = document.querySelector("#unity-canvas");
  const config = {
    dataUrl: "Build/docs.data",
    frameworkUrl: "Build/docs.framework.js",
    codeUrl: "Build/docs.wasm",
    streamingAssetsUrl: "StreamingAssets",
    companyName: "DefaultCompany",
    productName: "UPose",
    productVersion: "0.1.0",
  };

  const script = document.createElement("script");
  script.src = "Build/docs.loader.js";
  script.onload = () => {
    createUnityInstance(canvas, config).then((instance) => {
      window.unityInstance = instance;
    });
  };
  document.body.appendChild(script);
</script>
```

File names under `Build/` depend on your Unity WebGL product name. Match whatever the build actually emitted.

## Hiding the in-game UI (use your own buttons)

The viewer ships with on-canvas controls: dance list, left/right character arrows, Random, and pause/play. For an external site, hide that overlay and drive the scene from HTML.

**At load (URL flag):**

```
https://example.com/viewer/index.html?hideui=1
```

Accepted query flags: `hideui=1`, `hidegameui=1`, `hideui=true`, `gameui=0`, `ui=0`.

**From JavaScript after Unity is ready:**

```js
unityInstance.SendMessage("WebBridge", "HideGameUi");
```

| Call | What it hides |
| --- | --- |
| `HideGameUi` | Entire overlay: dance list, titles, Random, left, right, pause |
| `HideDanceList` | Dance list, Random, left, right, pause (titles stay) |
| `ShowGameUi` / `ShowDanceList` | Restore the matching overlay |

The 3D scene, avatar, and camera keep running. Your page buttons replace the hidden Unity buttons.

---

## In-game buttons → JavaScript

These HTML controls match the Unity overlay 1:1.

```html
<button onclick="send('PreviousCharacter')">Left character</button>
<button onclick="send('NextCharacter')">Right character</button>
<button onclick="send('ToggleRandomDanceMode')">Random</button>
<button onclick="send('TogglePause')">Pause / play</button>
<button onclick="send('PreviousDanceMove')">Previous dance</button>
<button onclick="send('NextDanceMove')">Next dance</button>
<button onclick="send('HideGameUi')">Hide Unity UI</button>
```

### Left / right character arrows

**JS**

```js
unityInstance.SendMessage("WebBridge", "PreviousCharacter");
unityInstance.SendMessage("WebBridge", "NextCharacter");
unityInstance.SendMessage("WebBridge", "SelectCharacter", "0"); // 0-based index
```

**What the C# does** (`WebBridge` → `CharacterSelector`)

- Finds the `CharacterSelector` in the scene.
- `PreviousCharacter` / `NextCharacter` cycles the active avatar, stops the previous dancer, and rebinds the dance menu to the new character.
- `SelectCharacter` jumps to a specific index (`"0"`, `"1"`, …).
- After a change, Unity pushes the character list back to the page (see [Character list callback](#character-list-callback)).

### Random

**JS**

```js
unityInstance.SendMessage("WebBridge", "ToggleRandomDanceMode"); // one random dance now
unityInstance.SendMessage("WebBridge", "StartRandomDanceMode");  // auto-pick every ~4s
unityInstance.SendMessage("WebBridge", "StopRandomDanceMode");
```

**What the C# does** (`RandomDanceMode`)

- `ToggleRandomDanceMode` is named like a toggle for historical reasons; it picks **one** random dance immediately and does **not** start auto-shuffle (`PlayRandomNow()`).
- `StartRandomDanceMode` enables a loop that selects a random menu dance about every 4 seconds.
- `StopRandomDanceMode` stops that loop. Manual dance changes also stop it.

### Pause / play

**JS**

```js
unityInstance.SendMessage("WebBridge", "TogglePause");
unityInstance.SendMessage("WebBridge", "PauseDance");
unityInstance.SendMessage("WebBridge", "ResumeDance");
unityInstance.SendMessage("WebBridge", "StopDance"); // full stop, does not resume
```

**What the C# does** (`InGamePausePlayButton` → `AvatarAnimationPlayer`)

- `TogglePause` is the in-game button: if dancing, freeze on the current pose and show the play icon; if paused, resume; if nothing is playing, replay the selected dance.
- `PauseDance` / `ResumeDance` are explicit. The pose stays on the avatar while paused.
- `StopDance` clears the clip. Resume will not bring it back; play a dance again.

### Next / previous dance

**JS**

```js
unityInstance.SendMessage("WebBridge", "NextDanceMove");
unityInstance.SendMessage("WebBridge", "PreviousDanceMove");
```

**What the C# does** (`AvatarUIItem`)

- Walks the in-game dance menu in order and plays the next/previous tile on the current character.
- If nothing is selected, next picks the first dance and previous picks the last.
- Stops random auto-shuffle so a manual pick is not overwritten.

---

## Full `SendMessage` reference

Object name is always `"WebBridge"`.

### Playback

| Method | Argument | Behavior |
| --- | --- | --- |
| `TogglePause` | none | Pause if playing, resume if paused, otherwise replay selected dance |
| `PauseDance` | none | Freeze current dance |
| `ResumeDance` | none | Continue from the paused pose, or replay selected dance if stopped |
| `StopDance` | none | Unload the dance (not a pause) |
| `NextDanceMove` | none | Next dance in the menu |
| `PreviousDanceMove` | none | Previous dance in the menu |
| `ToggleRandomDanceMode` | none | Play one random dance now |
| `StartRandomDanceMode` | none | Auto-cycle random dances |
| `StopRandomDanceMode` | none | Stop auto-cycle |

### Characters

| Method | Argument | Behavior |
| --- | --- | --- |
| `NextCharacter` | none | Same as the right arrow |
| `PreviousCharacter` | none | Same as the left arrow |
| `SelectCharacter` | `"0"`-based index | Jump to that avatar |
| `RenameCurrentAvatar` | display name | On-screen name only, max 48 chars |
| `RemoveCurrentAvatar` | `""` | Remove the current **custom** avatar (built-ins stay) |
| `RemoveCharacter` | `"0"`-based index | Remove that **custom** avatar |
| `ReportCharacters` | `""` | Re-send the character list to the page |

Built-in characters cannot be removed. Custom GLB avatars can.

### Overlay visibility

| Method | Argument | Behavior |
| --- | --- | --- |
| `HideGameUi` | none | Hide the entire Unity overlay |
| `ShowGameUi` | none | Show the overlay |
| `ToggleGameUi` | none | Toggle overlay |
| `SetGameUiVisible` | `"1"` / `"true"` / `"on"` / `"show"` to show; anything else hides | Set overlay |
| `HideDanceList` | none | Hide list + Random + arrows + pause |
| `ShowDanceList` | none | Show those controls |
| `ToggleDanceList` | none | Toggle |
| `SetDanceListVisible` | same flags as `SetGameUiVisible` | Set list/controls |

### Dances (names and list)

| Method | Argument | Behavior |
| --- | --- | --- |
| `RenameCurrentDance` | display name | Label only; playback still uses the file id |
| `RemoveCurrentDance` | `""` | Remove the selected dance for this session (built-ins return on refresh). At least one dance is kept. |

### Load an avatar

**From a URL** (JSON or two lines):

```js
unityInstance.SendMessage(
  "WebBridge",
  "LoadAvatarFromUrl",
  JSON.stringify({ name: "Ada", url: "https://example.com/ada.glb" })
);

// same thing as:
unityInstance.SendMessage("WebBridge", "LoadAvatarFromUrl", "Ada\nhttps://example.com/ada.glb");
```

**From a file picker** (bytes cannot go through `SendMessage`, so they are staged on `window` first):

```js
const file = document.querySelector("#glb-input").files[0];
window._pendingAvatarGlb = await file.arrayBuffer();
unityInstance.SendMessage("WebBridge", "LoadAvatarFromPendingFile", file.name.replace(/\.glb$/i, ""));
```

Unity reads `window._pendingAvatarGlb` via `PendingAvatarGlb.jslib`, copies it into C#, then clears it.

### Load a dance

Dance files are the project’s npz-json motion clips (same format as `StreamingAssets/atunda/`).

```js
unityInstance.SendMessage(
  "WebBridge",
  "LoadDanceFromUrl",
  JSON.stringify({ name: "Afefe", url: "https://example.com/Afefe.json" })
);

const file = document.querySelector("#dance-input").files[0];
window._pendingDanceJson = await file.arrayBuffer();
unityInstance.SendMessage("WebBridge", "LoadDanceFromPendingFile", file.name.replace(/\.json$/i, ""));
```

A successful load adds or selects a tile in the dance menu and starts playback on the current character.

---

## Character list callback

After character changes, Unity calls into the page if you define:

```js
window.atundaSetCharacters = function (names, selectedIndex, builtInCount) {
  const list = String(names || "").split("\n").filter(Boolean);
  // list[i] is the display name
  // selectedIndex is the current avatar
  // indices < builtInCount are built-in and cannot be removed
};
```

Implemented in `Assets/Plugins/WebGL/PendingAvatarGlb.jslib` as `AtundaReportCharacters`. Unity sends this shortly after startup and after next/previous/select/rename/load/remove.

---

## Minimal host page

```html
<!DOCTYPE html>
<html>
  <body>
    <canvas id="unity-canvas" width="960" height="600" tabindex="-1"></canvas>

    <p>
      <button data-cmd="PreviousCharacter">◀ Character</button>
      <button data-cmd="NextCharacter">Character ▶</button>
      <button data-cmd="ToggleRandomDanceMode">Random</button>
      <button data-cmd="TogglePause">Pause / Play</button>
      <button data-cmd="PreviousDanceMove">◀ Dance</button>
      <button data-cmd="NextDanceMove">Dance ▶</button>
    </p>

    <script>
      let unityInstance = null;
      const canvas = document.querySelector("#unity-canvas");

      function send(method, arg) {
        if (!unityInstance) return;
        if (arg === undefined) unityInstance.SendMessage("WebBridge", method);
        else unityInstance.SendMessage("WebBridge", method, String(arg));
      }

      document.querySelectorAll("[data-cmd]").forEach((btn) => {
        btn.addEventListener("click", () => send(btn.dataset.cmd));
      });

      window.atundaSetCharacters = function (names, selectedIndex) {
        console.log("characters", String(names).split("\n"), "selected", selectedIndex);
      };

      const script = document.createElement("script");
      script.src = "Build/docs.loader.js";
      script.onload = () => {
        createUnityInstance(canvas, {
          dataUrl: "Build/docs.data",
          frameworkUrl: "Build/docs.framework.js",
          codeUrl: "Build/docs.wasm",
          streamingAssetsUrl: "StreamingAssets",
          companyName: "DefaultCompany",
          productName: "UPose",
          productVersion: "0.1.0",
        }).then((instance) => {
          unityInstance = instance;
          send("HideGameUi");
        });
      };
      document.body.appendChild(script);
    </script>
  </body>
</html>
```

Replace `docs.*` with your build’s actual file names.

---

## Notes and limits

- **Same origin / CORS.** `LoadAvatarFromUrl` and `LoadDanceFromUrl` fetch from the browser. The file host must allow the viewer origin.
- **GLB avatars.** Custom characters are added at runtime; built-in ones stay after refresh. Custom ones do not persist across a reload unless you load them again.
- **Dances.** Built-in dances removed with `RemoveCurrentDance` come back on refresh. External dances exist only for the current session unless you load them again.
- **Display names** are sanitized (no `<` `>`, max 48 characters, single line).
- **Do not call `SendMessage` before** `createUnityInstance` resolves. Queue clicks until then.
- Source of truth for new methods: `Assets/WebBridge.cs`.
