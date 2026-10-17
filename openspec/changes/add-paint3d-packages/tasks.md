# Tasks: Add Paint3D packages

## 1. Core package (`com.ovs.paint3d`)
- [x] 1.1 Package manifest, runtime asmdef (Input System / uGUI version defines), editor asmdef, test asmdef.
- [x] 1.2 Brush model: `PaintBrush`, `BrushModifiers`, `ResolvedBrush`, `PaintBlendMode`.
- [x] 1.3 `Paintable` / `PaintableTexture`: allocation, property-block display, skinned baking, multi-slot.
- [x] 1.4 `PaintEngine`: texture-space drawing, blend-state materials, mirroring, UV mask, dilation.
- [x] 1.5 Shaders: `Paint3DCommon.cginc`, `Hidden/Paint3D/Sphere`, `Hidden/Paint3D/UVMask`, `Hidden/Paint3D/Dilate`.
- [x] 1.6 `PaintHistory` undo/redo.
- [x] 1.7 `PaintStorage` + automatic save/load on `Paintable`.
- [x] 1.8 `PaintRaycaster` (physics + exact triangle).
- [x] 1.9 `PaintPointerInput` + `PointerPainter` (live in-game painting).
- [x] 1.10 Editor: `Paint3DWindow` (scene painting), `PaintableEditor`, `PaintTextureExporter`.
- [x] 1.11 EditMode tests (brush resolve, mirroring, raycast math, storage keys).

## 2. Addons package (`com.ovs.paint3d.addons`)
- [x] 2.1 Package manifest + asmdefs.
- [x] 2.2 Decal painting: `DecalPaintOperation`, `Hidden/Paint3D/Decal`, `DecalPainter`, `PointerDecalPainter`.
- [x] 2.3 Dynamic painting: `DynamicPainter`.
- [x] 2.4 Particle collision painting: `ParticleCollisionPainter`.
- [x] 2.5 Physics collision painting: `CollisionPainter`.
- [x] 2.6 Color counting: `ColorCounting`, `PaintColorCounter`, `Hidden/Paint3D/CountPrepare`.
- [x] 2.7 VR: `XRPainter`.
- [x] 2.8 EditMode tests (color counting, decal bounds/reflection).

## 3. Integration & validation
- [x] 3.1 Register packages as `testables` in `Packages/manifest.json`.
- [x] 3.2 READMEs + CHANGELOGs.
- [ ] 3.3 Open the project in Unity 6000.4: confirm the packages compile, then run the EditMode tests in the Test Runner.
- [ ] 3.4 Manual smoke test in URP: paint a MeshRenderer and a skinned UMA character, check mirroring, undo/redo,
      save/load and the editor window. Make an Android build and a WebGL build.
