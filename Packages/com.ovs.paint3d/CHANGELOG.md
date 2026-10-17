# Changelog

## [0.1.0] - 2026-10-17

### Added
- `Paintable` and `PaintableTexture`: GPU paint textures for any shader property, with multi-texture and multi-material support, and skinned mesh baking.
- `PaintEngine`: texture-space GPU painting, smart mirroring, and seamless UV dilation.
- `PaintBrush`, `BrushModifiers`: pressure, jitter, spacing, gradual flow, and paint-through.
- `PaintHistory`: stroke-based undo and redo.
- `PaintStorage`: PNG save and load through a pluggable backend, with automatic save mode.
- `PaintRaycaster`: physics or exact-triangle raycasts that work without colliders.
- `PointerPainter`: live mouse, pen and touch painting for the Input System and the legacy Input Manager.
- Editor painter window, `Paintable` inspector, and PNG export.
