# Change: Add Paint3D core and addon packages

## Why
Directors need to customise props, sets and actors in the sandbox: paint a car, splatter a wall, put a sticker on a
costume or a tattoo on a character. They need to do it both while building a scene in the editor and at runtime,
on mobile, desktop, WebGL and VR. The project has no texture painting capability today.

## What Changes
- **New embedded package `com.ovs.paint3d` (Paint3D Core)**, assembly `Paint3D.Core` (+ `Paint3D.Core.Editor`):
  - In-game painting (`PointerPainter`) and in-editor painting (`Tools ▸ Paint3D ▸ Painter`).
  - Skinned mesh painting (posed `SkinnedMeshRenderer` baking).
  - Seamless UV painting (UV island mask + dilation pass).
  - Smart mirroring (`MirrorSettings`, per-paintable local space or shared pivot).
  - Undo & redo (`PaintHistory`, stroke-based GPU snapshots).
  - Automatic save & load (`Paintable.SaveMode`, `PaintStorage` PNG backend).
  - GPU accelerated texture-space painting (`PaintEngine`, no compute shaders).
  - Brush modifiers (pressure curve, jitter, spacing, gradual flow).
  - Paint any shader (any texture property, multi-texture, multi-material).
  - Live painting (property-block display, interpolated strokes, mouse/pen/touch).
  - Through painting & gradual painting (brush flags).
  - Cross-platform (desktop, mobile, WebGL2, XR).
- **New embedded package `com.ovs.paint3d.addons` (Paint3D Addons)**, assembly `Paint3D.Addons`, depends on core:
  - Decal painting (`DecalPaintOperation`, `DecalPainter`, `PointerDecalPainter`).
  - Dynamic painting (`DynamicPainter`).
  - Particle collision painting (`ParticleCollisionPainter`).
  - Physics collision painting (`CollisionPainter`).
  - Color counting (`PaintColorCounter`).
  - VR support (`XRPainter`, built-in `UnityEngine.XR` input).
- `Packages/manifest.json`: register both packages as `testables` so their EditMode tests run in the Test Runner.

## Impact
- Affected specs: `paint3d-core` (new), `paint3d-addons` (new).
- Affected code: new folders `Packages/com.ovs.paint3d/` and `Packages/com.ovs.paint3d.addons/`. No existing code
  is changed. The packages do not reference the game assemblies (`Systems.*`), so they can be reused in other
  projects.
