# Carbon World - Gemini Agent Instructions

## Code Style & Standards
- **Language:** C# 9.0+ (Unity 2022/6000+)
- **Formatting:** K&R style braces (standard C#). 4 spaces indentation.
- **Comments:** minimal, explain *why* not *what*. Avoid "divider" comments (e.g., `// Fields`).
- **Namespaces:** Match folder structure (e.g., `CarbonWorld.Core.Systems`). Pluralize namespaces (e.g., `CarbonWorld.Features.Inventories`) to avoid naming collisions with singular class names.
- **Using Directives:** Place `using` statements **outside** (above) the `namespace` declaration.

## Naming Conventions
- **Classes/Structs/Enums:** `PascalCase`
- **Public Properties/Methods/Fields:** `PascalCase`
- **Private Serialized Fields (`[SerializeField]`):** `camelCase` (e.g., `myPrefab`)
- **Private Non-Serialized Fields:** `_camelCase` (e.g., `_gameState`)
- **Local Variables:** `camelCase`
- **Parameters:** `camelCase`
- **Constants:** `UPPER_SNAKE_CASE` or `PascalCase` (depending on usage context).

## Unity Specifics
- **Serialization:** Use `[SerializeField] private` instead of `public` for Inspector variables.
- **Attributes:** Place `[SerializeField]`, `[Header]`, etc. above the field, not inline.
- **Methods:** `Awake` -> `OnEnable` -> `Start` -> `Update` -> `OnDisable` -> `OnDestroy`.
- **Editor Logic:** Wrap editor-only logic (like finding assets in database) in `#if UNITY_EDITOR`.

## Architecture
- **Managers vs Systems:**
  - **Systems:** Persistent, Global (`DontDestroyOnLoad`), Singleton. Handle data & state.
  - **Managers:** Scene-specific. Handle objects & flow within a scene.
- **Data:** Prefer `ScriptableObject` for static data configuration.
  - **Databases:** Use `ScriptableObject` + Odin Inspector to manage lists of assets (e.g., Blueprints, Scenarios).

## UI Development (UI Toolkit)
- **Framework:** Use **UI Toolkit** (UIDocument, UXML, USS) over legacy IMGUI or UGUI (Canvas).
- **Styling:** 
  - **NO inline styles** in C# or UXML.
  - Use external `.uss` stylesheets for all styling.
  - Use classes for reusable styles.
- **Structure:** Separate structure (UXML) from style (USS) and logic (C#).

## Tools & Libraries
- **Odin Inspector:** 
  - Use `[Title("...")]` for grouping fields in the Inspector.
  - Use `[Button]` for exposing editor actions.
  - Use `[OnCollectionChanged]` for list/array callbacks.
  - Prioritize Odin attributes over standard Unity `[Header]` or `[Space]` when possible.¨
- **Odin Validator:**
- **Aline (Drawing):**
  - Use `using Drawing;` for runtime and editor visualization.
  - Use `Draw.ingame` for runtime debug lines/shapes.
  - Use `Draw.xy` or `Draw.xz` for 2D/3D specific drawing.
  - Prefer Aline over `Debug.DrawLine` or `Gizmos` for complex visualizations.
- **Core Packages:**
  - **URP:** Project uses Universal Render Pipeline.
  - **Input System:** Use New Input System (`com.unity.inputsystem`).
  - **Cinemachine:** For camera control.
  - **AI Navigation:** For pathfinding/agents.

## File Structure
- one class per file.
- filename matches class name.
