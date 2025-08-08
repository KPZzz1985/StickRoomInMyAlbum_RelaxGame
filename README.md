<<<<<<< HEAD
# StickRoomInMyAlbum_RelaxGame

Unity-based engine for an interactive sticker room game, inspired by "My Sticker Room".

## Description
This project uses specially structured PSD files to automatically import and parse:
- Background layers (`BG_`)
- Sticker groups (`STK_`) with nested `Base`, `Overlay` and `Place_Zone`
- Global sticker placement shadows (`PLACER_STK_`)

In the scene:
1. The full PSD prefab is instantiated, preserving original hierarchy.
2. Background and global shadow placeholders appear.
3. The `Stickers_Stash` group is hidden and used to populate a scrollable inventory.
4. Drag & drop lets players place stickers onto matching shadows, activating nested zones and overlays.

## Development Plan
1. Define PSD layer naming conventions and grouping
2. Import PSD using Unity 2D PSD Importer
3. Develop `PsdProcessorWindow` editor script to extract metadata
4. Create `PsdMetadata` structure (ScriptableObject)
5. Instantiate and initialize scene (`SceneInitializer`)
6. Generate scrollable inventory UI
7. Implement drag & drop (`StickerDragHandler`, `PlaceholderArea`)
8. Manage sticker placement logic and nested zones
9. Implement placement animations (prefab swaps)
10. Handle removal and return to inventory
11. Save/load placement state
12. Performance testing and optimization

---

© 2025 StickRoomInMyAlbum Development Team
=======
# StickRoomInMyAlbum_RelaxGame
>>>>>>> origin/main
