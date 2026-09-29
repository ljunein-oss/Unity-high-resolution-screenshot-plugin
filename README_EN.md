# High-Res Screenshot

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)

English | [中文](README.md)

A small Unity editor window for high resolution screenshots, roughly what HighResShot does in Unreal.

```
Tools ▸ High-Res Screenshot
```

## What it does

1. Source: the game camera (matches the Game view) or the current scene view camera.
2. Resolution: a multiplier of the view (1x to 8x), or explicit width and height with 1080p / 4K / 8K shortcuts.
3. Format: PNG, or EXR if you want to keep HDR values for grading later.
4. Output: pick a folder, a filename prefix and a three digit counter. The last shot is previewed in the window.
5. The size is capped by `SystemInfo.maxTextureSize`; above that it gets clamped and warns you.

## Install

Package Manager ▸ `+` ▸ Add package from git URL:

```
https://github.com/ljunein-oss/Unity-high-resolution-screenshot-plugin.git
```

Pin a version with a tag:

```
https://github.com/ljunein-oss/Unity-high-resolution-screenshot-plugin.git#v1.0.0
```

Or just copy the script from `Editor` into your project's `Assets/Editor`.

## Notes

- Rendering offscreen does **not** include Screen Space Overlay UI. For that, use Unity's own screenshot or switch the canvas to Screen Space Camera.
- There is no tiled rendering, so anything above the GPU's max texture size gets clamped. 8K already depends on your card.
- Outside play mode the game camera shot is whatever that camera sees in the scene (no runtime animation or physics state).
- With the game camera, post processing follows that camera's own setting. With the scene view camera, a URP camera data component is added temporarily and post processing is switched on.
- Settings live in EditorPrefs, per project.

## Compatibility

| | Version |
|---|---|
| Unity | 2021.3+ (only tested on 2022.3 here) |
| Pipeline | Built-in, URP and HDRP all work (no SRP package dependency) |

## License

MIT
