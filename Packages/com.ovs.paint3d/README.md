# Paint3D Core (`com.ovs.paint3d`)

GPU accelerated texture painting on 3D meshes for Unity, in game and in the editor.

The optional addons package (`com.ovs.paint3d.addons`) adds decals, dynamic painting, particle and physics
collision painting, color counting and VR on top of this core.

## Features

| Feature | Where |
|---|---|
| In-game & in-editor painting | `PointerPainter` (runtime), `Tools ▸ Paint3D ▸ Painter` window (editor, edit or play mode) |
| Skinned mesh painting | `Paintable` bakes `SkinnedMeshRenderer` poses, so animated characters can be painted |
| Seamless UV painting | Per-texture UV island mask plus a dilation pass after each dab (`Paintable.Seamless`) |
| Smart mirroring | `MirrorSettings`: mirror across X/Y/Z of each paintable's local space or a shared pivot |
| Undo & redo | `PaintHistory`: stroke-based GPU snapshots, Ctrl/Cmd+Z and Ctrl/Cmd+Y |
| Automatic save & load | `Paintable.SaveMode = Automatic`, PNG via `PaintStorage` (backend can be swapped) |
| Consistent performance (GPU) | `PaintEngine` renders dabs in texture space with vertex/fragment shaders |
| Brush modifiers | `BrushModifiers`: pressure curve, radius/opacity/hue jitter, spacing, gradual flow |
| Paint any shader | `PaintableTexture` names any texture property, on any material index (multi-texture, multi-material) |
| Live painting | Paint textures are shown through property blocks right away. Strokes are interpolated, and works with mouse, pen and touch |
| Through & gradual painting | `PaintBrush.PaintThrough` turns off the facing test; `BrushModifiers.Gradual` builds paint up over time |
| Cross-platform | No compute shaders, no `CopyTexture`, ARGB32 render textures. Runs on desktop, mobile, WebGL2 and XR |

## Quick start

1. Add **Paintable** to a GameObject that has a `MeshRenderer` + `MeshFilter` or a `SkinnedMeshRenderer`.
   The default slot paints `_BaseMap` (URP) and falls back to `_MainTex` (Built-in).
2. Add **Pointer Painter** to any GameObject (it uses `Camera.main` by default) and press Play.
3. Paint with the left mouse button, a pen or touch. Ctrl/Cmd+Z undoes and Ctrl/Cmd+Y redoes.

### In-editor painting

`Tools ▸ Paint3D ▸ Painter` → **Start Painting**. The selected paintables are painted, or every paintable in the
scene when nothing is selected. Use **Export PNGs** (or **Export & Assign To Materials**) to keep the result.
Edit-mode paint textures are released on domain reload, so export before you recompile.

### Scripting

```csharp
using Paint3D;

// Paint a sphere wherever you like
var brush = new PaintBrush { Color = Color.cyan, Radius = 0.1f };
PaintEngine.PaintAll(new SpherePaintOperation(worldPoint, rayDirection, brush.Resolve()));

// Undo a group of dabs as one step
PaintHistory.BeginStroke();
PaintEngine.Paint(paintable, new SpherePaintOperation(p, dir, brush.Resolve()), new MirrorSettings(MirrorAxes.X));
PaintHistory.EndStroke();
PaintHistory.Undo();

// Persist
paintable.Save();
paintable.Load();
```

Write your own brush shapes by deriving from `PaintOperation`. Your shader should use `Paint3DVert` from
`Packages/com.ovs.paint3d/Runtime/Resources/Paint3D/Paint3DCommon.cginc`.

## How it works

1. Each `PaintableTexture` allocates an ARGB32 render texture, copies the original texture into it, and hands it to
   the renderer through a `MaterialPropertyBlock`. Shared materials are never modified.
2. A dab draws the mesh with the vertex shader outputting **UV coordinates as clip-space positions**. The fragment
   shader then receives each texel's world position and normal and checks them against the brush. Hardware blending
   applies the blend mode.
3. A dilation pass, driven by a UV island mask that is rendered once, copies border texels outward. This keeps
   bilinear filtering and mipmaps from pulling unpainted texels across seams.
4. `PaintHistory` copies a texture the first time a stroke touches it. Undo blits the copy back.

## Notes

- **Raycasting:** colliders are used when present. Otherwise the raycaster tests the paint mesh's triangles exactly.
  That works in edit mode and on posed skinned meshes, but static meshes need **Read/Write** enabled in player builds.
- **Memory:** each slot uses 2 render textures and an R8 mask. Each undo step holds one copy per touched texture.
  On mobile, keep `PaintHistory.MaxSteps` and texture resolutions modest.
- **WebGL:** `persistentDataPath` is backed by IndexedDB. Set a custom `PaintStorage.Backend` if you need a
  different store.
- **SRP Batcher:** renderers that have a property block skip the SRP Batcher. They still render correctly.
