# High-Res Screenshot

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)

[English](README_EN.md) | 中文

一个小巧的 Unity 编辑器窗口，用来出高分辨率截图，思路和 UE 的 HighResShot 差不多。窗口界面支持**中文 / English 切换**。

```
工具 ▸ 高分辨率截图
Tools ▸ High-Res Screenshot
```

## 能做什么

1. 取景来源：游戏相机（和 Game 视图一致），或者当前场景视图的机位。
2. 分辨率：按视图倍数（1x ~ 8x），或者直接填宽高，带 1080p / 4K / 8K 快捷键。
3. 格式：PNG，或者 EXR（保留 HDR 亮度，方便丢进后期）。
4. 输出：自选文件夹 + 文件名前缀 + 三位递增；拍完可以在窗口里预览，也可以只存不打开。
5. 分辨率上限受 `SystemInfo.maxTextureSize` 限制，超了会被夹住并给出提示。

## 安装

Package Manager ▸ `+` ▸ Add package from git URL：

```
https://github.com/ljunein-oss/Unity-high-resolution-screenshot-plugin.git
```

要固定版本就带上 tag：

```
https://github.com/ljunein-oss/Unity-high-resolution-screenshot-plugin.git#v1.0.0
```

或者直接把 `Editor` 里的脚本拷到工程的 `Assets/Editor`。

## 说明

- 相机离屏渲染**不含 Screen Space Overlay 的 UI**，要连 UI 一起截得用 Unity 自带的截图，或者把 Canvas 改成 Screen Space Camera。
- 没有做分块渲染，所以超过显卡最大纹理边长的分辨率会被夹住（8K 已经要看显卡了）。
- 编辑器不在播放模式时，游戏相机拍到的就是它在场景里的视角（没有运行时的动画/物理状态）。
- 用游戏相机拍时，后处理跟随这台相机自己的设置；用场景视图机位时会临时补一个 URP 相机数据并打开后处理。
- 设置存在 EditorPrefs 里，跟着工程走。

## 兼容性

| | 版本 |
|---|---|
| Unity | 2021.3 及以上（我这边只测过 2022.3） |
| 渲染管线 | Built-in / URP / HDRP 都能用（不依赖任何 SRP 包） |

## License

MIT
