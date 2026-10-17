# Spec: Paint3D Addons

## ADDED Requirements

### Requirement: Decal Painting
The addons SHALL project a texture onto surfaces inside an oriented box. Texture alpha SHALL mask the paint and the brush color SHALL tint it. Decals SHALL be stampable from scripts, from a transform (`DecalPainter`) or from pointer input (`PointerDecalPainter`).

#### Scenario: Stamp a sticker
- **WHEN** the user clicks on a paintable with a `PointerDecalPainter` that has a decal texture
- **THEN** the decal is projected once at the hit point, oriented to the surface normal

### Requirement: Dynamic Painting
The addons SHALL let a moving object paint every paintable it passes through. Movement between frames SHALL be interpolated.

#### Scenario: Tire tracks
- **WHEN** a `DynamicPainter` moves quickly across a paintable floor
- **THEN** a continuous trail is painted along its path

### Requirement: Particle Collision Painting
The addons SHALL paint at particle collision points, optionally using particle color, speed-scaled radius, or a decal.

#### Scenario: Paint gun
- **WHEN** particles from a system with `ParticleCollisionPainter` hit a paintable wall
- **THEN** paint is applied at each collision intersection, up to the per-frame cap

### Requirement: Physics Collision Painting
The addons SHALL paint at rigidbody contact points on the other object, on itself, or on both. Radius and opacity SHALL scale with impact impulse.

#### Scenario: Ball impact
- **WHEN** a ball with `CollisionPainter` hits a paintable floor above the minimum impulse
- **THEN** a mark sized by the impulse appears at the contact point on the floor

### Requirement: Color Counting
The addons SHALL report, for a palette of colors, the fraction of a paintable texture's UV-covered texels that match each color. The readback SHALL be asynchronous where supported.

#### Scenario: Team coverage
- **WHEN** half of a wall's surface is painted red and the counter palette contains red
- **THEN** `GetRatio("Red")` returns approximately 0.5 after the next count

### Requirement: VR Support
The addons SHALL paint with XR controllers through the built-in XR input API. They SHALL support contact and ray modes, trigger pressure, haptic feedback and undo/redo on controller buttons.

#### Scenario: Contact painting in VR
- **WHEN** the user touches a paintable with the brush tip and pulls the trigger halfway
- **THEN** paint is applied at the tip with pressure-scaled strength, and the controller vibrates
