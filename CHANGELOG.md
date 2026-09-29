# Changelog

## [1.1.0] - 2026-09-29

- 窗口内新增中文 / English 切换（记在 EditorPrefs 里，跟着工程走）
- 菜单同时注册了 `Tools ▸ High-Res Screenshot` 和 `工具 ▸ 高分辨率截图`

## [1.0.0] - 2026-09-29

首个版本。

- 游戏相机 / 场景视图机位两种取景来源
- 按倍数（1x ~ 8x）或指定宽高（含 1080p / 4K / 8K 快捷键）出图
- PNG 与 EXR 两种格式
- 输出文件夹、文件名前缀、三位递增编号，窗口内预览
- 分辨率按 SystemInfo.maxTextureSize 自动夹住并提示
- 不依赖任何 SRP 包，Built-in / URP / HDRP 通用
