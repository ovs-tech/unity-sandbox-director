# Copilot Instructions for **Ero Director (Mobile & AR Sandbox Edition)**

## 1. Project Overview

- **Genre:** Adult sandbox simulation (Ero Director).
- **Platforms:** Mobile-first, extendable to PC, VR, AR via DLC.
- **Core Concept:** Player acts as a director, creating erotic/romantic scenarios with actors, objects, and AR/VR immersion.
- **Engine:** Unity (C#).
- **Character System:** UMA (Unity Multipurpose Avatar).
- **Architecture:** Modular & DLC-ready (AR/VR/Mobile input, Assets, Gameplay Extensions).

## 2. Technical Guidelines

- Use **Unity Input System (.inputactions)** with separate maps:
  - `UIAndGameplayInputActions` (menu navigation, timeline editor, interaction, camera, actor control, play/record).
- Apply **Command Pattern** for editor + gameplay actions (undo/redo, binding, track operations).
- Timeline: JSON-based tracks, each track can bind **actor / object / camera**.
- DLC system should be implemented as **modular packages** (ScriptableObjects or Addressables).

## 3. Coding Style

- Language: **C#** (Unity).
- Follow Unity coding conventions:
  - Classes `PascalCase`.
  - Fields `_camelCase`.
  - Public properties `PascalCase`.
- Prefer **dependency injection** (Zenject or custom) for extensibility.
- Ensure **async/await** support for loading assets and DLC.
- Avoid hardcoding input or assets → always reference via configs/ScriptableObjects.

## 4. Copilot Hints

When generating code:

- Always create **separate handlers** for UI and Gameplay inputs.
- Use **interfaces (`IInputHandler`, `IDLCModule`)** for extensibility.
- Use **ScriptableObject** for configs (actors, scenes, DLC metadata).
- Prefer **editor tooling** (custom inspectors, timeline editor windows).
- For UMA integration: generate characters via UMA APIs, don’t write custom mesh generators.
- Don't create document when don't the task

## 5. Example Tasks Copilot Should Support

- Generate **InputAction asset handlers** (`UIInputHandler`, `GameplayInputHandler`).
- Create **DLC loader template** (`IDLCModule`, `DLCManager`).
- Implement **track system** (`Track`, `ActorTrack`, `CameraTrack`) in JSON.
- Extend **Timeline Editor UI** with UnityEditor + IMGUI/UIToolkit.
- Suggest clean **Command Pattern implementation** for undo/redo in editor.
