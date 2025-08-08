using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class SceneInitializer : MonoBehaviour
{
    // Store initial local scales for animation
    private Dictionary<string, Vector3> initialScales = new Dictionary<string, Vector3>();
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
    private Transform stashGroupTransform;

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
        // Cache reference to Stickers_Stash group
        stashGroupTransform = roomRootTransform.Find("Stickers_Stash");

        // Record initial scales and hide each sticker in stash at startup
        if (stashGroupTransform != null)
        {
            foreach (Transform stk in stashGroupTransform)
            {
                // Record initial scale of the actual sprite leaf (Base/Overlay)
                var leafSr = FindSpriteRendererInChildren(stk);
                if (leafSr != null)
                    initialScales[stk.name] = leafSr.transform.localScale;
                else
                    initialScales[stk.name] = Vector3.one;
                // Hide entire sticker group initially
                stk.gameObject.SetActive(false);
            }
        }
        
        // Register placeholder drop handlers by scanning all SpriteRenderers
        var allSR = roomRoot.GetComponentsInChildren<SpriteRenderer>();
        foreach (var srItem in allSR)
        {
            if (srItem.gameObject.name.StartsWith("PLACER_STK_"))
            {
                var placeholderObj = srItem.gameObject;
                if (placeholderObj.GetComponent<PlaceholderArea>() == null)
                    placeholderObj.AddComponent<PlaceholderArea>();
                // Automatically add a BoxCollider2D trigger matching the sprite bounds for touch/drop
                if (placeholderObj.GetComponent<BoxCollider2D>() == null)
                {
                    var box = placeholderObj.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    var sprite = srItem.sprite;
                    if (sprite != null)
                    {
                        // Set collider size and offset based on sprite bounds
                        box.size = sprite.bounds.size;
                        box.offset = sprite.bounds.center;
                    }
                }
            }
        }
        // Setup inventory counter
        var stash = stashGroupTransform;
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
        var stashGroup = stashGroupTransform;
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

    // Called by PlaceholderArea when a sticker is dropped; activates existing sticker from stash
    public void PlaceSticker(string stickerId, Transform placeholderTransform)
    {
        // Locate sticker group by ID
        if (stashGroupTransform == null) return;
        var stkObj = stashGroupTransform.Find(stickerId);
        if (stkObj == null) return;
        // Activate sticker group
        stkObj.gameObject.SetActive(true);
        stkObj.name = stickerId + "_Placed";
        // Gather Base and Overlay sprite transforms
        var primaryTransforms = new List<Transform>();
        // Base: find SpriteRenderer whose sprite name matches stickerId
        var allSr = stkObj.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer srBase = null;
        foreach (var sr in allSr)
        {
            if (sr.sprite != null && sr.sprite.name == stickerId)
            {
                srBase = sr;
                break;
            }
        }
        if (srBase != null)
            primaryTransforms.Add(srBase.transform);
        // Overlay (if any)
        var overlayGroup = stkObj.Find("Overlay");
        if (overlayGroup != null)
        {
            var srOverlay = FindSpriteRendererInChildren(overlayGroup);
            if (srOverlay != null)
                primaryTransforms.Add(srOverlay.transform);
        }
        // Gather Place_Zone_x sprite transforms for all Place_Zone groups
        var zoneTransforms = new List<Transform>();
        foreach (Transform grp in stkObj)
        {
            if (grp.name.StartsWith("Place_Zone"))
            {
                foreach (Transform child in grp)
                {
                    var zSr = FindSpriteRendererInChildren(child);
                    if (zSr != null)
                        zoneTransforms.Add(zSr.transform);
                }
            }
        }
        // Initialize scales to zero
        foreach (var tf in primaryTransforms) tf.localScale = Vector3.zero;
        foreach (var tf in zoneTransforms) tf.localScale = Vector3.zero;
        // Start animated placement: base+overlay in parallel, then zones sequentially
        StartCoroutine(AnimatePlacement(primaryTransforms, zoneTransforms, stickerId));

        // Remove placeholder
        placeholderTransform.gameObject.SetActive(false);

        // Update counter
        usedCount++;
        if (counterText != null)
            counterText.text = $"{usedCount}/{totalCount}";
    }

    // Animate leaf sprite scale from 0 up to its recorded value over 1 second
    private IEnumerator AnimateSticker(Transform leafTf, string id)
    {
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 targetScale = initialScales.ContainsKey(id) ? initialScales[id] : Vector3.one;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            leafTf.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }
        leafTf.localScale = targetScale;
        // Clear animation flag if used in future
        var animator = leafTf.GetComponent<StickerAnimator>();
        if (animator != null)
            animator.isStickerAnimation = false;
    }

    /// <summary>
    /// Animates the 'stick' growing: primary (Base+Overlay) in parallel, then Place_Zone items sequentially.
    /// </summary>
    private IEnumerator AnimatePlacement(List<Transform> primary, List<Transform> zones, string id)
    {
        float duration = 0.15f;
        Vector3 targetScale = initialScales.ContainsKey(id) ? initialScales[id] : Vector3.one;
        // Parallel animation for primary
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            var scale = Vector3.Lerp(Vector3.zero, targetScale, t);
            foreach (var tf in primary)
                tf.localScale = scale;
            yield return null;
        }
        foreach (var tf in primary) tf.localScale = targetScale;
        // Sequential animation for zones
        foreach (var tf in zones)
        {
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tf.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                yield return null;
            }
            tf.localScale = targetScale;
        }
    }

    /// <summary>
    /// Recursively finds the first SpriteRenderer in children.
    /// </summary>
    private SpriteRenderer FindSpriteRendererInChildren(Transform parent)
    {
        if (parent == null) return null;
        var sr = parent.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            return sr;
        foreach (Transform child in parent)
        {
            var cs = FindSpriteRendererInChildren(child);
            if (cs != null) return cs;
        }
        return null;
    }
}
