# Placement Editor Framework Design

## Overview
The Placement Editor Framework is a robust, "easy build" Unity Editor extension designed to simplify the creation and management of placement systems for building games. It aims to reduce friction for new users by automating tedious tasks (like socket generation) and providing a centralized hub for configuring placeable objects and the core controller.

## Architecture & Workflow

The framework follows a "Hub" architecture where complex creation tasks are handled in a dedicated window, while ongoing tweaks and runtime debugging are handled in clean custom inspectors.

### 1. Prefab Creator Tool (The "Hub" Window)
A centralized Editor Window (`Window -> Placement System -> Prefab Creator`) designed to guide users from a raw 3D model to a fully validated, connectible placement asset.

**Features:**
* **Source Setup:** Drag-and-drop a source GameObject or Prefab. Automatically attaches `Part` and a Collider if missing.
* **Socket Management:** List view of all attached sockets. Add/delete/edit sockets (`SocketType`, position, rotation).
* **Auto-Socket Generation (Magic Button):** Reads mesh bounds and automatically places sockets on standard faces (Top, Bottom, Left, Right, Front, Back) using a predefined `SocketType`, enabling 1-click building block creation.
* **Validation Rules:** An easy-to-use list to assign `PlacementRule` ScriptableObjects.
* **Ghost Generator:** A one-click button that clones the hierarchy, removes colliders, strips out heavy scripts, assigns a default "Ghost Material", and saves it as a dedicated Ghost prefab.

### 2. Part Editor
A clean, optimized Inspector view for prefabs containing the `Part` component.

**Features:**
* **Summary View:** Read-only list of configured rules and the exact number of attached sockets.
* **Open in Hub:** A prominent "Open in Prefab Creator" button to direct complex editing to the dedicated Hub window.
* **Interactive Sockets (Scene View):** Custom handles in the Scene View to visually drag and adjust socket offsets directly on the object without opening the Hub.

### 3. Placement Controller Editor
A categorized Inspector that simplifies the configuration of the `PlacementController`.

**Features:**
* **Categorized Foldouts:** Separates configuration into logical groups: Core Setup (Object to place, Camera), Placement Settings (Snap range, Raycast), Selection Settings (Layer masks), and Events.
* **Runtime Debugging:** In Play Mode, displays real-time stats (Current Active Tool, Ghost Object Name, Placed Objects count).

### 4. "Quick Start" Scene Setup Wizard
A menu action (`Tools -> Placement System -> Add To Scene`) that instantly scaffolds a new scene for placement.
* Automatically creates a `Placement Manager` GameObject.
* Attaches `PlacementController`, a default strategy, visualizer, and wires up `Camera.main` and input providers for zero-setup execution.

### 5. Visual "Socket Matcher" Window (Future/Optional)
A utility to drag two prefabs into slots to verify if their sockets can connect, aiding in rule debugging without requiring Play Mode.

## Success Criteria
* A user can drag a 1x1x1 cube into the Hub, click auto-generate sockets, click generate ghost, and immediately start building with it in a Quick-Start scene.
* The inspector for `PlacementController` hides complexity behind logical foldouts instead of a massive list of fields.
