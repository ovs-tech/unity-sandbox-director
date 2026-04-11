# ProjectCard Design

## Context

The current `ProjectCard` component is still implemented as a compact horizontal row with a small thumbnail, title, and meta text. The attached `Projects.html` reference and the current projects page styling point toward a different presentation: a vertical grid card with a large image area, optional badge, top-right menu affordance, gradient overlay, and compact metadata underneath.

This design updates `ProjectCard` so it matches the approved visual direction while staying reusable inside the existing `ListProjectPage` and `ProjectItemView` flow.

## Goals

- Replace the current row-style `ProjectCard` layout with a grid card that visually matches the approved HTML reference.
- Support only the standard project card variant used for real project entries.
- Keep badge text parent-driven and optional.
- Keep the top-right menu button inside the component and let the parent own behavior when it is clicked.
- Support both direct code-behind configuration and existing binding-oriented usage.
- Preserve `ProjectCard` as a presentational component, without page, navigation, or store logic.

## Non-Goals

- No draft or empty-state variant in this change.
- No project filtering, search, navigation, or menu action logic inside `ProjectCard`.
- No business-rule inference inside `ProjectCard` for deciding whether a project is `VR`, `AR`, `NEW`, or any other badge.
- No animation work beyond what is already feasible in the current UI Toolkit styling model.

## Recommended Approach

Implement `ProjectCard` as a self-contained UI Toolkit component with:

- A richer internal visual tree in UXML.
- Card-specific styling in USS based on the approved HTML structure.
- A small public API for image, badge, and menu state.
- Existing text binding support preserved for title and meta.

This keeps the component reusable and prevents page-level logic from leaking into the card.

## Component Structure

The new `ProjectCard` visual tree should contain these parts:

1. Root card container
2. Media container with a portrait-style aspect ratio close to `4:5`
3. Background image element
4. Optional badge element in the top-left corner, hidden by default
5. Menu button in the top-right corner
6. Bottom gradient overlay on the media region
7. Content area under the media
8. Title label
9. Meta row label

The root remains a `VisualElement`-based custom element loaded from `ProjectCard.uxml`.

### Required element names

Use stable queryable names for internal elements so the C# API can target them consistently:

- media container: `card-media`
- background image: `card-image`
- badge container or label: `card-badge`
- menu button: `card-menu-button`
- gradient overlay: `card-gradient`
- title label: `project-name`
- meta label: `project-meta`

## Public API

`ProjectCard` should expose a mixed binding and code-behind contract.

### Binding-friendly data

- `project-name`
- `project-meta`

These remain available for UXML attributes and/or parent-driven data binding so the current `ProjectItemView` data-source flow does not need to be replaced outright.

### Code-behind configuration

- `Initialize(string projectName, string meta = null, Texture2D image = null)`
- `SetImage(Texture2D image)`
- `SetBadge(string text, bool visible)`
- `SetMenuVisible(bool visible)`

Implementation defaults:

- `SetBadge(string text, bool visible)` should update the `card-badge` text and use layout-collapsing visibility when hidden
- `SetMenuVisible(bool visible)` should show or hide `card-menu-button` using layout-collapsing visibility rather than reserving visible empty space
- `Initialize(...)` should remain a convenience method for title, meta, and image only

### Events / callbacks

The component should surface an explicit menu interaction hook owned by the parent. The preferred contract is a simple C# event:

- `public event Action MenuClicked;`

`ProjectCard` should raise this event when `card-menu-button` is clicked. Parent views should subscribe after creating the component, typically in the owning view constructor or setup path, not inside the component itself.

## Ownership Boundaries

### Owned by `ProjectCard`

- Internal element queries and references
- Showing and hiding the badge element
- Setting badge text
- Showing and hiding the menu button
- Applying the image as a background
- Updating the title and meta labels
- Encapsulating card-only presentation details

### Owned by parent views

- Mapping domain data to card properties
- Deciding whether a badge is shown
- Choosing badge text
- Deciding whether the menu button should be visible
- Handling what happens when the menu button is clicked
- Navigation and context-menu actions

## Integration Plan

`ProjectItemView` should continue to create and own a `ProjectCard` instance.

Recommended integration path:

1. Keep `m_ProjectCard.dataSource = m_ViewModel?.Project` for title/meta binding compatibility.
2. Add lightweight configuration in `ProjectItemView.BindDataContext()` only for card presentation state that is actually available at that call site.
3. Keep `ListProjectPage` responsible only for list creation, filtering, and page-level actions.

Because the current `Project` model only exposes `name` and `meta`, the first implementation may leave badge hidden by default and keep the menu visible by default until richer project metadata exists. The integration layer must not fabricate domain state just to satisfy the component API.

In the current phase, `ProjectItemView` should keep the component on safe defaults if no richer data exists:

- do not invent badge data for the current `Project` model
- either avoid calling the badge API or call `SetBadge(string.Empty, false)`
- keep the menu visible by default, or explicitly call `SetMenuVisible(true)` once during binding

This minimizes churn and keeps the new card compatible with the current screen architecture.

## Styling Direction

The USS should follow the approved HTML reference closely, adapted to Unity UI Toolkit:

- Rounded outer card container
- Dark surface background matching the current projects screen palette
- Large image region with rounded corners
- Top-left badge with high contrast text and compact pill styling
- Top-right circular menu affordance
- Compact title and muted meta text below the image
- Enough spacing to read cleanly in a two-column grid

The card should be designed to look correct inside the current `ListView`-based projects layout without embedding layout policy that belongs to the page.

### Visual constants

Use the attached HTML reference as the source of truth for the first implementation.

- Card outer padding: `8px`
- Card border radius: `12px`
- Card surface color: close to `rgb(54, 30, 37)`
- Media corner radius: `8px`
- Title font size: `14px`, bold
- Meta font size: `12px` or the nearest UI Toolkit equivalent
- Meta text color: muted light text aligned with the current projects palette

### Aspect ratio enforcement

The media region should use a fixed portrait presentation rather than a free-growing flex height. The preferred first pass is a card width near `220px` with a media height near `275px`, preserving an approximate `4:5` relationship. If the page-level grid width changes, the implementation should preserve the portrait ratio as closely as UI Toolkit allows.

For the first implementation, prefer explicit width and height values in USS for the media region rather than introducing custom layout code.

### Badge styling

Badge defaults:

- hidden by default
- positioned at top `8px`, left `8px` within the media region
- horizontal padding near `8px`
- vertical padding near `2px`
- border radius near `6px`
- bold uppercase text near `10px`
- default background close to `rgba(0, 0, 0, 0.6)`
- default text color: white

The component may support an alternate highlighted badge style later, but the first implementation only needs the default treatment unless the parent explicitly requires another variation.

### Menu button styling

Menu button defaults:

- visible by default
- positioned at top `8px`, right `8px` within the media region
- circular hit area approximately `24px` to `28px`
- background close to `rgba(0, 0, 0, 0.4)`
- white icon or label representing the vertical overflow affordance, equivalent to `more_vert`

Exact hover animation parity with the HTML is not required for the first implementation. Priority goes to layout, contrast, and a clear click target.

### Gradient overlay

The gradient overlay should sit over the lower portion of the media region.

- visual emphasis should run from darker at the bottom to transparent toward the top
- bottom color near `rgba(0, 0, 0, 0.8)`
- top color fully transparent
- occupied height roughly the lower third of the media region

If UI Toolkit cannot reproduce the exact gradient expression from the HTML, a visually similar darker lower overlay is acceptable.

## Data Flow

Runtime flow should be:

1. Parent creates `ProjectCard`
2. Parent binds or initializes text values
3. Parent sets image if one is available
4. Parent sets badge text and visibility when needed
5. Parent chooses whether the menu button is visible
6. Parent subscribes to the menu click interaction

`ProjectCard` must not derive badge state from the domain model on its own.
If the parent has no badge-related data, the badge remains hidden.

For the current codebase, `ProjectItemView.BindDataContext()` should continue to rely on `dataSource` for `project-name` and `project-meta`, and only call presentation methods for explicit defaults, such as leaving the badge hidden and menu visible.

## Error Handling and Fallbacks

- Missing image should leave a styled fallback background instead of breaking layout.
- Empty meta text should not break spacing or alignment.
- Hidden badge state should fully collapse the badge visually.
- Hidden menu state should not reserve unnecessary visible affordance.
- `Initialize` should remain safe when called with partial or null data.

## Testing Strategy

The implementation should be covered with focused UI component tests where practical.

### Component structure checks

- `ProjectCard` clones successfully from UXML
- Required elements can be queried: image, badge, menu, title, meta

### State behavior checks

- `Initialize` updates title and meta correctly
- `SetImage` updates the media element background
- `SetBadge` updates text and visibility correctly
- `SetMenuVisible` updates menu visibility correctly
- Menu button interaction raises the component menu event once per click
- Partial or null input does not throw

### Integration regression checks

- `ProjectItemView` can still create and bind a `ProjectCard`
- The updated card does not rely on the old row layout assumptions

## Risks

- The current `ListView` wrapping behavior may still need minor layout tuning after the card becomes visually larger.
- UI Toolkit styling capabilities may require small deviations from the HTML hover behavior.
- If parent views do not configure badge/menu state explicitly, the new card should still render safely with sensible defaults.

## Implementation Notes

- Keep the change focused on the existing `ProjectCard` component and only the minimal integration points needed to support it.
- Preserve existing public behavior where possible, but update layout and API where necessary to support the approved design.
- Avoid introducing page-specific knowledge into the component.