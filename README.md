# StickRoomInMyAlbum (Relax Game)

## Overview
StickRoomInMyAlbum is a Unity-based engine for a sticker-room experience, inspired by “My Sticker Room” (https://play.google.com/store/apps/details?id=com.playstrom.my.sticker.room). Players collect and place decorative stickers onto a room background. Core features:

- Import a specially structured PSD file with layers for background, shadows (ghost outlines), and hidden sticker objects.
- Automatically parse PSD layers into Unity GameObjects, recording sprite, position, and sorting order.
- Generate a three-slot carousel inventory UI with click navigation, highlight overlays, and a used/total sticker counter.
- Allow only the center slot icon to be interactable (draggable).
- Drag & drop stickers from inventory onto matching shadow outlines in world space, snapping them into place and activating nested placeholders or overlays.
- Support nested `Place_Zone` containers: placeholders within stickers only appear after their parent is placed.
- Trigger wave animations for carousel icons and bounce animations for sticker placements using UniTask.
- Use custom shaders: `Custom/SpriteDistortion` for drag distortion, `UI/AlphaMaskWhite` for highlight overlays.
- Full touch input support for mobile devices (Android/iOS).
- Asynchronous operations use UniTask; UI text uses TextMeshPro.

---

## PSD Layer Conventions
To enable correct parsing, PSD files must follow this structure at the top level:

1. **BG_Room**
   - Contains all background elements (GameObjects). Each child’s ordering in the PSD determines its render sorting order.

2. **Previous_Sticker_Shadow_Placer**
   - Contains ghost-outline layers with names like `PLACER_STK_<id>_<name>`, visible at scene start. Each placeholder corresponds to a hidden sticker in `Stickers_Stash`.

3. **Stickers_Stash**
   - Hidden at runtime; contains groups `STK_<id>_<name>` for each sticker. Each sticker group may contain:
     - **Base**: the main sticker sprite (required).
     - **Overlay**: an optional top layer (e.g., cabinet door) hidden until placement.
     - **Place_Zone**: an optional child group containing nested placeholders `PLACER_STK_<id>_<subname>` for further stickers.

Naming conventions:
- Prefix background groups with `BG_`.
- Prefix shadow outlines with `PLACER_STK_`.
- Prefix sticker groups with `STK_`.
- Child layers inside stickers: `Base`, `Overlay`, `Place_Zone`.

---

## Architecture

### 1. PSD Import & Metadata Generation
**Script**: `Assets/Editor/PsdProcessorWindow.cs`
- Configures `TextureImporter` for multiple sprites and layer-group import.
- Instantiates the imported PSD prefab in Editor, locates `BG_Room`, `Previous_Sticker_Shadow_Placer`, and `Stickers_Stash` groups.
- Recursively finds `SpriteRenderer` in children to capture sprite asset, position, and sorting order.
- Builds a `PsdMetadata` asset (`Assets/Scripts/PsdMetadata.cs`) containing:
  - `List<PsdLayerData> backgroundLayers`
  - `List<PsdLayerData> zoneLayers` (global shadows)
  - `List<PsdStickerData> stickerDatas` (per-sticker base, nested zones, nested overlays)

**Data Structures**:
- `PsdLayerData` (id, layerPrefab, sprite, position, sortingOrder)
- `PsdStickerData` (id, basePrefab, baseSprite, basePosition, baseSortingOrder, nestedZoneLayers, nestedOverlayLayers)

### 2. Runtime Scene Initialization
**Script**: `Assets/Scripts/SceneInitializer.cs`
- Public `PsdMetadata` reference; UI fields: `inventoryContent` (RectTransform), `inventoryIconPrefab`, `counterText` (TextMeshProUGUI).
- `Start()`:
  1. Center and scale room (`transform.localPosition = (0, -2.23, 0)`, `scale = 0.32`).
  2. Instantiate `metadata.psdPrefab` under this GameObject.
  3. Hide `Stickers_Stash` group.
  4. Add `PlaceholderArea` to all `PLACER_STK_` GameObjects found in children (including inactive).
  5. Build inventory icons:
     - Iterate hidden `STK_` groups under `Stickers_Stash`.
     - Find `Base` child’s `SpriteRenderer.sprite`.
     - Instantiate UI icon, set its `Image.sprite`, add `StickerDragData` referencing the runtime `stkGroup.gameObject` and its ID.
     - Hide the sticker in the scene initially.
  6. Initialize counters (used/total stickers).

- `PlaceSticker(GameObject stickerPrefab, Vector3 dropPosition)`:
  - Activates the sticker GameObject and sets its world position.
  - Enables its nested `Place_Zone` and `Overlay` children if present.
  - Updates used counter.

### 3. Drag & Drop & Placeholder Logic
- **`PlaceholderArea`** (`Assets/Scripts/PlaceholderArea.cs`): implements `IDropHandler`.
  - On drop, retrieves `StickerDragData.stickerPrefab`, calls `SceneInitializer.PlaceSticker`, and deactivates itself.

- **`StickerDragData`** (`Assets/Scripts/StickerDragData.cs`): stores `stickerId` and `stickerPrefab` (runtime GameObject).

- **`StickerDragHandler`** (`Assets/Scripts/StickerDragHandler.cs`): implements `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler` on UI icon:
  - Manages drag visuals, raycast blocking, and resetting if drop fails.

### 4. Inventory Carousel UI
- **`InventoryCarouselWithPrefabs`** (`Assets/Scripts/InventoryCarouselWithPrefabs.cs`)
  - Manages a three-slot carousel inventory (leftSlot, centerSlot, rightSlot).
  - Instantiates `InventoryIcon` prefab (Image, StickerDragData, StickerDragHandler, CanvasGroup) and fits sprites proportionally.
  - Creates highlight overlays via `CreateHighlight` with `UI/AlphaMaskWhite` shader.
  - Only the center icon is draggable; other icons are static.
  - Navigation via `prevButton`/`nextButton` shifts `currentIndex`, calls `UpdateSlots`, and disables buttons briefly.
  - Wave animations (`AnimateWaveNext`, `AnimateWavePrev`) pulse icons and highlights sequentially with `ScalePulse` (UniTask).
  - `UseCurrent` removes the placed sticker and refills the carousel, triggering wave animation from the appropriate side.

---

## Project Structure
- Assets/Editor: Unity Editor scripts for PSD import and metadata generation.
- Assets/Scripts: Core runtime scripts (SceneInitializer, PlaceholderArea, StickerDragData, StickerDragHandler, InventoryCarouselWithPrefabs).
- Assets/Shaders: Custom shaders (`Custom/SpriteDistortion`, `UI/AlphaMaskWhite`).
- Assets/PSDs: Source PSD files.
- Assets/Metadata: Generated ScriptableObjects (`PsdMetadata`).
- Assets/Prefabs: UI prefabs (`InventoryIcon`, etc.).

## Dependencies & Setup
- Unity 2020.3 LTS (or newer) with 2D PSD Importer package installed.
- `Cysharp.Threading.Tasks` (UniTask) for async routines.
- TextMeshPro package for UI text.
- Unity UI (Canvas, ScrollRect, Image, etc.).

**Installation**:
1. Clone or open repository.
2. In Unity, import 2D PSD Importer via Package Manager.
3. Place your structured PSD files in `Assets/PSDs/`.
4. Open **StickerRoom → PSD Processor** window (from the Editor menu) and click **Process All PSDs**.
5. Create or locate the generated `*.asset` in `Assets/Metadata/`; assign it to `SceneInitializer`.
6. In the scene, set up Canvas with `ScrollRect` (horizontal) using `inventoryContent` and `inventoryIconPrefab` (a prefab with `Image`, `CanvasGroup`, `StickerDragHandler`, `StickerDragData`).
7. Assign the `counterText` field to a `TextMeshProUGUI` element.
8. Enter Play mode.

---

## Development Plan & Roadmap

### Completed Tasks
- [x] Define PSD naming conventions and top-level groups.
- [x] Editor script imports PSD and generates `PsdMetadata`.
- [x] ScriptableObject structures (`PsdLayerData`, `PsdStickerData`, `PsdMetadata`).
- [x] Runtime instantiation and hiding of `Stickers_Stash`.
- [x] Recursive placeholder registration for `PLACER_STK_` layers.
- [x] Inventory UI creation from hidden stickers, correct sprite linkage.
- [x] Core drag & drop and sticker placement logic.

### In Progress
- [ ] Drag & drop UI polish (scrollable inventory, icon layout, used/total counter).

### Pending Tasks
1. [ ] Implement sticker removal and return to inventory, restoring placeholders.
2. [ ] Persist sticker placement state (save & load) using JSON or `PlayerPrefs`.
3. [ ] Trigger animated prefabs on correct placement (replace static sprite).
4. [ ] Performance testing & optimization (UniTask usage, batching, texture memory).
5. [ ] Final UI polish: animations, feedback, sound effects.

---

## Contribution & License
Feel free to fork and submit pull requests. Please maintain coding conventions and update this README with new instructions.

License: MIT
