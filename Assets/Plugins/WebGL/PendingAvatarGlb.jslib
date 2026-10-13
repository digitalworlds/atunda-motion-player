mergeInto(LibraryManager.library, {
  GetPendingAvatarGlbLength: function () {
    if (typeof window === "undefined" || !window._pendingAvatarGlb) {
      return 0;
    }
    return window._pendingAvatarGlb.byteLength | 0;
  },

  ConsumePendingAvatarGlb: function (destPtr, maxLen) {
    if (typeof window === "undefined" || !window._pendingAvatarGlb) {
      return 0;
    }

    var src = new Uint8Array(window._pendingAvatarGlb);
    var n = src.length < maxLen ? src.length : maxLen;
    HEAPU8.set(src.subarray(0, n), destPtr);
    window._pendingAvatarGlb = null;
    return n;
  },

  GetPendingDanceJsonLength: function () {
    if (typeof window === "undefined" || !window._pendingDanceJson) {
      return 0;
    }
    return window._pendingDanceJson.byteLength | 0;
  },

  ConsumePendingDanceJson: function (destPtr, maxLen) {
    if (typeof window === "undefined" || !window._pendingDanceJson) {
      return 0;
    }

    var src = new Uint8Array(window._pendingDanceJson);
    var n = src.length < maxLen ? src.length : maxLen;
    HEAPU8.set(src.subarray(0, n), destPtr);
    window._pendingDanceJson = null;
    return n;
  },

  AtundaReportCharacters: function (namesPtr, selectedIndex, builtInCount) {
    var names = UTF8ToString(namesPtr);
    if (typeof window.atundaSetCharacters === "function") {
      window.atundaSetCharacters(names, selectedIndex, builtInCount);
    }
  }
});
