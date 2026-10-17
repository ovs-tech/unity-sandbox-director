# Paint3D Addons (`com.ovs.paint3d.addons`)

Optional painters built on [Paint3D Core](../com.ovs.paint3d/README.md). Each addon is a standalone component.
They all use the core's brush, mirroring, undo/redo and save/load.

| Feature | Component | Notes |
|---|---|---|
| Decal painting | `DecalPainter`, `PointerDecalPainter`, `DecalPaintOperation` | Box-projected textures: stickers, stamps, text, footprints. Can align to the view or the stroke, or roll randomly |
| Dynamic painting | `DynamicPainter` | A moving object paints whatever it passes through. Movement is interpolated, and animated targets are supported |
| Particle collision painting | `ParticleCollisionPainter` | Paints at particle impacts. Can use the particle color, scale with speed, or stamp a decal |
| Physics collision painting | `CollisionPainter` | Paints at rigidbody contacts, on the other object, itself or both. Strength scales with impulse |
| Color counting | `PaintColorCounter` | Coverage ratio per palette color, read back from the GPU asynchronously (synchronous fallback on WebGL) |
| VR support | `XRPainter` | Any XR controller through `UnityEngine.XR`. Contact or ray mode, trigger pressure, haptics, buttons for undo and redo |

## Quick start

- **Decals:** add `PointerDecalPainter` instead of `PointerPainter` and assign a texture with alpha.
- **Paint gun:** add `ParticleCollisionPainter` to a particle system whose Collision module is set to *World*.
- **Tire tracks:** put `DynamicPainter` on a child at the wheel's contact point, and turn off *Paint Through* on its brush.
- **Splat coverage:** add `PaintColorCounter` next to a `Paintable`, then read `GetRatio("Red")` or listen to `Counted`.
- **VR:** put `XRPainter` on the controller object (or assign a tip transform) and pick the hand node.
