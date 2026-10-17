# Design: Paint3D packages

## Context
Painting must run on mobile GPUs and WebGL2, where compute shaders are missing or unreliable. It must also handle
animated (skinned) characters, any material and shader, and work in edit mode without touching shared materials.

## Goals / Non-Goals
- Goals: GPU painting that looks the same on every platform; a small core with an extension point; addons as
  separate, optional components.
- Non-goals: painting normal maps with tangent-space correctness, UDIM/multi-tile UVs, networked/multiplayer sync
  (it can be built on `PaintEngine` later).

## Decisions
- **Texture-space rasterisation.** The vertex shader writes `uv * 2 - 1` as the clip position, so each texel gets
  its surface world position and normal. A brush is then a world-space function (sphere, decal box) that the
  fragment shader evaluates. This handles skinned meshes (bake the pose, then draw), any UV layout, and through
  painting. Hardware blending implements the blend modes. Blend state lives on cached materials because property
  blocks cannot change render state.
- **Display through `MaterialPropertyBlock` per material index.** Shared materials are never instanced or modified,
  which makes edit-mode painting safe. Trade-off: those renderers skip the SRP Batcher.
- **Seams.** An R8 UV island mask is rendered once per slot. After every dab, a two-ring dilation pass copies island
  border texels outward and the buffers are swapped.
- **Extension point: `PaintOperation`.** It supplies a shader, world bounds, properties and a reflection. The engine
  handles slots, groups, mirroring, history and seams. Addons add operations (decals) and painters (collision,
  particles, XR) without changing core code.
- **History** snapshots each touched texture once per stroke (`BeginStroke`/`EndStroke`), capped by `MaxSteps`.
  Painting outside a stroke (particles, collisions) is not recorded, which keeps memory bounded.
- **Raycasting** prefers physics colliders, so non-paintable colliders occlude. Without colliders it falls back to an
  exact triangle test on the paint mesh, which works in edit mode and on posed skinned meshes.
- **Input** goes through `PaintPointerInput`, which uses `versionDefines` to support both the Input System and the
  legacy Input Manager. Shaders sit in package `Resources` folders so player builds always include them.

## Risks / Trade-offs
- Memory: 2 RGBA render textures plus an R8 mask per slot, plus history snapshots. Mitigation: per-slot resolution
  and `PaintHistory.MaxSteps`.
- A skinned bake per paint frame costs CPU. Mitigation: at most one bake per frame per paintable.
- Triangle raycasts in players need Read/Write meshes. Mitigation: use colliders, or enable Read/Write. This is
  documented.

## Open Questions
- Should the MiniTimeline record paint strokes for playback? (Possible follow-up.)
- Should saved paint be stored inside the project JSON (`Systems.Persistence`) instead of separate PNGs? This can be
  done with a custom `IPaintStorageBackend`.
