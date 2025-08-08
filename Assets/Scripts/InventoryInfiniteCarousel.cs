using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages an infinite, three-slot inventory carousel. When a sticker is used,
/// one of the side slots moves to center (random), and a new random sticker fills the vacant side.
/// </summary>
public class InventoryInfiniteCarousel : MonoBehaviour
{
    [Header("Stash Container (all spawned icons)")]
    [SerializeField] private RectTransform content; // where SceneInitializer places all inventory icons

    [Header("UI Slots")]
    [SerializeField] private Image leftSlot;
    [SerializeField] private Image centerSlot;
    [SerializeField] private Image rightSlot;
    [Header("Navigation Buttons or Zones")]
    [SerializeField] private UnityEngine.UI.Button prevButton;
    [SerializeField] private UnityEngine.UI.Button nextButton;

    private List<StickerDragData> stashData = new List<StickerDragData>();
    private System.Random rng = new System.Random();

    private void Awake()
    {
        // Ensure slots have StickerDragData, and center slot has drag handler
        if (leftSlot != null && leftSlot.GetComponent<StickerDragData>() == null)
            leftSlot.gameObject.AddComponent<StickerDragData>();
        if (centerSlot != null)
        {
            if (centerSlot.GetComponent<StickerDragData>() == null)
                centerSlot.gameObject.AddComponent<StickerDragData>();
            if (centerSlot.GetComponent<StickerDragHandler>() == null)
                centerSlot.gameObject.AddComponent<StickerDragHandler>();
        }
        if (rightSlot != null && rightSlot.GetComponent<StickerDragData>() == null)
            rightSlot.gameObject.AddComponent<StickerDragData>();
    }

    private System.Collections.IEnumerator Start()
    {
        // Wait one frame for SceneInitializer to populate content
        yield return null;
        // Gather stash and hide originals
        if (content != null)
        {
            stashData.Clear();
            foreach (Transform child in content)
            {
                var data = child.GetComponent<StickerDragData>();
                var img = child.GetComponent<Image>();
                if (data != null && img != null)
                {
                    stashData.Add(data);
                    child.gameObject.SetActive(false);
                }
            }
        }
        // Initialize three visible slots
        AssignRandomToSlot(leftSlot);
        AssignRandomToSlot(centerSlot);
        AssignRandomToSlot(rightSlot);
        // Bind navigation buttons
        if (prevButton != null)
            prevButton.onClick.AddListener(PrevSlot);
        if (nextButton != null)
            nextButton.onClick.AddListener(NextSlot);
    }

    /// <summary>
    /// Assigns a random sticker from the stash to the given UI slot.
    /// </summary>
    private void AssignRandomToSlot(Image slot)
    {
        if (slot == null || stashData.Count == 0) return;
        int idx = rng.Next(stashData.Count);
        var data = stashData[idx];
        // Set sprite
        slot.sprite = data.GetComponent<Image>().sprite;
        // Update drag data
        var sd = slot.GetComponent<StickerDragData>();
        sd.stickerId = data.stickerId;
        sd.stickerPrefab = data.stickerPrefab;
        // Enable raycast on center slot only
        if (slot == centerSlot)
            slot.raycastTarget = true;
        else
            slot.raycastTarget = false;
    }

    /// <summary>
    /// Called after a sticker is successfully dropped in the scene.
    /// Moves a random side slot to center and refills the empty side.
    /// </summary>
    public void OnStickerUsed()
    {
        if (centerSlot == null || stashData.Count == 0) return;
        // Randomly pick left (true) or right (false)
        bool pickLeft = rng.NextDouble() < 0.5;
        Image picked = pickLeft ? leftSlot : rightSlot;
        // Move picked to center
        centerSlot.sprite = picked.sprite;
        var pickedData = picked.GetComponent<StickerDragData>();
        var sdCenter = centerSlot.GetComponent<StickerDragData>();
        sdCenter.stickerId = pickedData.stickerId;
        sdCenter.stickerPrefab = pickedData.stickerPrefab;
        // Refill the side we picked
        AssignRandomToSlot(pickLeft ? leftSlot : rightSlot);
    }
    /// <summary>Move left slot to center, refill left slot.</summary>
    public void PrevSlot()
    {
        if (stashData.Count == 0) return;
        // Move left to center
        centerSlot.sprite = leftSlot.sprite;
        var leftData = leftSlot.GetComponent<StickerDragData>();
        var sdCenter = centerSlot.GetComponent<StickerDragData>();
        sdCenter.stickerId = leftData.stickerId;
        sdCenter.stickerPrefab = leftData.stickerPrefab;
        // Refill left
        AssignRandomToSlot(leftSlot);
    }
    /// <summary>Move right slot to center, refill right slot.</summary>
    public void NextSlot()
    {
        if (stashData.Count == 0) return;
        // Move right to center
        centerSlot.sprite = rightSlot.sprite;
        var rightData = rightSlot.GetComponent<StickerDragData>();
        var sdCenter = centerSlot.GetComponent<StickerDragData>();
        sdCenter.stickerId = rightData.stickerId;
        sdCenter.stickerPrefab = rightData.stickerPrefab;
        // Refill right
        AssignRandomToSlot(rightSlot);
    }
}
