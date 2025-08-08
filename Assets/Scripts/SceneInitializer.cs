using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class SceneInitializer : MonoBehaviour
{
    public PsdMetadata metadata;
    [Header("(Removed) Scene Parents - not used")]
    // Scene prefab hierarchy is instantiated directly
    [Header("Inventory UI")]
    [SerializeField] private RectTransform inventoryContent; // Content under ScrollRect
    [SerializeField] private GameObject inventoryIconPrefab; // prefab with Image component
    [SerializeField] private TextMeshProUGUI counterText; // UI Text for counter
    private int totalCount;
    private int usedCount;
    private Transform roomRootTransform;

    void Start()
    {
        // Instantiate the full PSD prefab with original hierarchy
        if (metadata == null)
        {
            Debug.LogError("SceneInitializer: PsdMetadata not assigned!");
            return;
        }
        // Center and scale the room
        transform.localPosition = new Vector3(0f, -2.23f, 0f);
        transform.localScale = Vector3.one * 0.335f;
        var roomRoot = Instantiate(metadata.psdPrefab, transform);
        roomRoot.name = metadata.psdName + "_Runtime";
        roomRootTransform = roomRoot.transform;
        // Hide the full-size sticker stash group
        var stashGroup = roomRootTransform.Find("Stickers_Stash");
        if (stashGroup != null) stashGroup.gameObject.SetActive(false);
        
        // Register placeholder drop handlers by scanning all SpriteRenderers
        var allSR = roomRoot.GetComponentsInChildren<SpriteRenderer>();
        foreach (var srItem in allSR)
        {
            if (srItem.gameObject.name.StartsWith("PLACER_STK_"))
            {
                var placeholderObj = srItem.gameObject;
                if (placeholderObj.GetComponent<PlaceholderArea>() == null)
                    placeholderObj.AddComponent<PlaceholderArea>();
            }
        }
        // Setup inventory counter
        var stash = stashGroup;
        totalCount = stash != null ? stash.childCount : 0;
        usedCount = 0;
        if (counterText != null)
            counterText.text = $"{usedCount}/{totalCount}";
        // Initialize inventory UI from stickerDatas
        InitializeInventory();
    }

    void InitializeInventory()
    {
        if (roomRootTransform == null) return;
        var stashGroup = roomRootTransform.Find("Stickers_Stash");
        if (stashGroup == null) return;

        totalCount = stashGroup.childCount;
        usedCount = 0;
        if (counterText != null)
            counterText.text = $"{usedCount}/{totalCount}";

        foreach (Transform stkGrp in stashGroup)
        {
            // find Base sprite renderer by matching sprite.name to group name
            var srs = stkGrp.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer srBase = null;
            foreach (var sr in srs)
            {
                if (sr.sprite != null && sr.sprite.name == stkGrp.name)
                {
                    srBase = sr;
                    break;
                }
            }
            if (srBase == null) continue;

            // Create inventory icon
            var icon = Instantiate(inventoryIconPrefab, inventoryContent);
            var img = icon.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.sprite = srBase.sprite;

            // Add drag data and handler
            var data = icon.GetComponent<StickerDragData>() ?? icon.AddComponent<StickerDragData>();
            data.stickerId = stkGrp.name;
            data.stickerPrefab = stkGrp.gameObject;
            if (icon.GetComponent<StickerDragHandler>() == null)
                icon.AddComponent<StickerDragHandler>();
        }
    }

    // Called by PlaceholderArea when a sticker is dropped
    public void PlaceSticker(GameObject stickerPrefab, Vector3 dropPosition)
    {
        // Instantiate the sticker from stash
        var go = Instantiate(stickerPrefab, transform);
        go.name = stickerPrefab.name + "_Placed";
        go.transform.position = dropPosition;
        // Activate its nested Place_Zone and Overlay
        var placeZone = go.transform.Find("Place_Zone");
        if (placeZone != null)
            placeZone.gameObject.SetActive(true);
        var overlay = go.transform.Find("Overlay");
        if (overlay != null)
            overlay.gameObject.SetActive(true);
        // Deactivate the placeholder sprite
        // Original placeholder under Previous_Sticker_Shadow_Placer
        var px = metadata.psdName + "_Runtime";
        var rootRt = transform.Find(px);
        if (rootRt != null)
        {
            var prev = rootRt.Find("Previous_Sticker_Shadow_Placer");
            if (prev != null)
            {
                var placeholderName = "PLACER_" + stickerPrefab.name.Replace("STK_", "STK_");
                var placeholder = prev.Find(placeholderName);
                if (placeholder != null)
                    placeholder.gameObject.SetActive(false);
            }
        }
        // Update counter
        usedCount++;
        if (counterText != null)
            counterText.text = $"{usedCount}/{totalCount}";
    }
}
