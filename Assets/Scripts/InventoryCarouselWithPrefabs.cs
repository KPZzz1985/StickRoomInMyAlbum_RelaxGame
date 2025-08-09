using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// Prefab-based infinite three-slot carousel using InventoryIcon prefab.
/// Preserves the original sequence from inventory content. Center slot is draggable.
/// </summary>
public class InventoryCarouselWithPrefabs : MonoBehaviour
{
    // Highlights under each slot icon
    private GameObject leftHighlight, centerHighlight, rightHighlight;
    [Header("Highlight Material")]
    [Tooltip("Material using UI/AlphaMaskWhite shader for white alpha mask highlight")]
    [SerializeField] private Material highlightMaterial;
    [Header("Highlight Settings")]
    [Tooltip("Scale multiplier for highlight overlay relative to slot size")]
    [SerializeField] private float highlightScale = 1.3f;

    [Header("Inventory Data")]
    [Tooltip("Container where SceneInitializer spawns raw inventory icons. These will be hidden.")]
    [SerializeField] private RectTransform content;

    [Header("Slot Containers")]
    [SerializeField] private RectTransform leftSlot;
    [SerializeField] private RectTransform centerSlot;
    [SerializeField] private RectTransform rightSlot;

    [Header("Navigation Buttons or Zones")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Icon Prefab")]
    [Tooltip("InventoryIcon prefab with Image, StickerDragData, StickerDragHandler, CanvasGroup")]
    [SerializeField] private GameObject iconPrefab;
    [Header("Timing Settings")]
    [Tooltip("Duration of each pulse in seconds (scale up + down)")]
    [SerializeField] private float pulseDuration = 0.5f;
    [Tooltip("Delay between pulse stages in seconds")]
    [SerializeField] private float stageDelay = 0.5f;
    [Tooltip("Disable nav buttons for this duration after click")]
    [SerializeField] private float disableDuration = 0.75f;

    private class Entry { public string id; public GameObject prefab; public Sprite sprite; }
    private List<Entry> stash = new List<Entry>();
    private int currentIndex = 0;
    private GameObject leftInst, centerInst, rightInst;

    private IEnumerator Start()
    {
        // Wait frame for SceneInitializer
        yield return null;
        // Gather stash entries and hide originals
        if (content != null)
        {
            stash.Clear();
            foreach (Transform child in content)
            {
                var data = child.GetComponent<StickerDragData>();
                var img = child.GetComponent<Image>();
                if (data != null && img != null)
                {
                    stash.Add(new Entry { id = data.stickerId, prefab = data.stickerPrefab, sprite = img.sprite });
                    child.gameObject.SetActive(false);
                }
            }
        }
        // Bind navigation
        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrev);
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNext);
        // Initial populate
        UpdateSlots();
    }

    private void ClearSlots()
    {
        if (leftHighlight != null) Destroy(leftHighlight);
        if (leftInst != null) Destroy(leftInst);
        if (centerHighlight != null) Destroy(centerHighlight);
        if (centerInst != null) Destroy(centerInst);
        if (rightHighlight != null) Destroy(rightHighlight);
        if (rightInst != null) Destroy(rightInst);
    }

    private void UpdateSlots()
    {
        ClearSlots();
        int count = stash.Count;
        if (count == 0) return;
        // Left slot: only if previous exists (highlight then icon)
        if (currentIndex > 0)
        {
            int leftIdx = currentIndex - 1;
            leftHighlight = CreateHighlight(leftSlot, stash[leftIdx].sprite);
            leftInst = Instantiate(iconPrefab, leftSlot, false);
            SetupSlot(leftInst, stash[leftIdx], draggable: false);
        }
        // Center slot: always (highlight then icon)
        centerHighlight = CreateHighlight(centerSlot, stash[currentIndex].sprite);
        centerInst = Instantiate(iconPrefab, centerSlot, false);
        SetupSlot(centerInst, stash[currentIndex], draggable: true);
        // Right slot: only if next exists (highlight then icon)
        if (currentIndex + 1 < count)
        {
            int rightIdx = currentIndex + 1;
            rightHighlight = CreateHighlight(rightSlot, stash[rightIdx].sprite);
            rightInst = Instantiate(iconPrefab, rightSlot, false);
            SetupSlot(rightInst, stash[rightIdx], draggable: false);
        }
    }

    private void SetupSlot(GameObject inst, Entry e, bool draggable)
    {
        var img = inst.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = e.sprite;
            // Fit sprite inside container, preserving aspect ratio
            var instRt = inst.GetComponent<RectTransform>();
            float containerWidth = instRt.rect.width;
            float containerHeight = instRt.rect.height;
            float spriteWidth = e.sprite.rect.width;
            float spriteHeight = e.sprite.rect.height;
            float spriteAspect = spriteWidth / spriteHeight;
            float containerAspect = containerWidth / containerHeight;
            var imgRt = img.rectTransform;
            if (spriteAspect > containerAspect)
            {
                // Sprite is wider relative to container: fit width
                imgRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerWidth);
                imgRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerWidth / spriteAspect);
            }
            else
            {
                // Sprite is taller relative to container: fit height
                imgRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerHeight);
                imgRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerHeight * spriteAspect);
            }
        }
        var data = inst.GetComponent<StickerDragData>();
        if (data != null)
        {
            data.stickerId = e.id;
            data.stickerPrefab = e.prefab;
        }
        var handler = inst.GetComponent<StickerDragHandler>();
        if (handler != null)
            handler.enabled = draggable;
        var cg = inst.GetComponent<CanvasGroup>();
        if (cg != null)
            cg.blocksRaycasts = draggable;
    }

    private void OnPrev()
    {
        if (stash.Count == 0) return;
        // Move left if possible
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateSlots();
            // disable buttons to prevent rapid clicks
            prevButton.interactable = false;
            nextButton.interactable = false;
            _ = ReenableButtonsAsync();
            AnimateWavePrev().Forget();
        }
    }

    private void OnNext()
    {
        if (stash.Count == 0) return;
        // Move right if possible
        if (currentIndex < stash.Count - 1)
        {
            currentIndex++;
            UpdateSlots();
            // disable buttons to prevent rapid clicks
            prevButton.interactable = false;
            nextButton.interactable = false;
            _ = ReenableButtonsAsync();
            AnimateWaveNext().Forget();
        }
    }

    /// <summary>Call after a sticker is placed to remove it and refill carousel.</summary>
    public void UseCurrent()
    {
        if (stash.Count == 0) return;
        // Determine direction: if removal not at end, next item shifts into center (from right)
        int oldIndex = currentIndex;
        stash.RemoveAt(currentIndex);
        // If empty after removal, clear
        if (stash.Count == 0)
        {
            ClearSlots();
            return;
        }
        // After removal, if oldIndex is within new range, fill from right; otherwise, from left
        bool fromRight = oldIndex < stash.Count;
        // Adjust currentIndex if we removed the last element
        if (currentIndex >= stash.Count)
            currentIndex = stash.Count - 1;
        UpdateSlots();
        // Play wave from appropriate side
        if (fromRight)
            AnimateWaveNext().Forget();
        else
            AnimateWavePrev().Forget();
    }

    // Wave animation when pressing Next: right->center->left with explicit delays
    private async UniTaskVoid AnimateWaveNext()
    {
        // stage 1
        if (rightInst != null && rightHighlight != null)
        {
            _ = ScalePulse(rightHighlight.transform, 0.5f, pulseDuration);
            _ = ScalePulse(rightInst.transform, 0.5f, pulseDuration);
        }
        // wait for next stage
        await UniTask.Delay(TimeSpan.FromSeconds(stageDelay));
        // stage 2
        if (centerInst != null && centerHighlight != null)
        {
            _ = ScalePulse(centerHighlight.transform, 1.25f, pulseDuration);
            _ = ScalePulse(centerInst.transform, 1.25f, pulseDuration);
        }
        await UniTask.Delay(TimeSpan.FromSeconds(stageDelay));
        // stage 3
        if (leftInst != null && leftHighlight != null)
        {
            _ = ScalePulse(leftHighlight.transform, 1.5f, pulseDuration);
            _ = ScalePulse(leftInst.transform, 1.5f, pulseDuration);
        }
    }

    // Wave animation when pressing Prev: left->center->right with explicit delays
    private async UniTaskVoid AnimateWavePrev()
    {
        // stage 1
        if (leftInst != null && leftHighlight != null)
        {
            _ = ScalePulse(leftHighlight.transform, 0.5f, pulseDuration);
            _ = ScalePulse(leftInst.transform, 0.5f, pulseDuration);
        }
        await UniTask.Delay(TimeSpan.FromSeconds(stageDelay));
        // stage 2
        if (centerInst != null && centerHighlight != null)
        {
            _ = ScalePulse(centerHighlight.transform, 1.25f, pulseDuration);
            _ = ScalePulse(centerInst.transform, 1.25f, pulseDuration);
        }
        await UniTask.Delay(TimeSpan.FromSeconds(stageDelay));
        // stage 3
        if (rightInst != null && rightHighlight != null)
        {
            _ = ScalePulse(rightHighlight.transform, 1.5f, pulseDuration);
            _ = ScalePulse(rightInst.transform, 1.5f, pulseDuration);
        }
    }

    // Scale pulse: scale to factor and back over totalDuration (half up and half down)
    private async UniTask ScalePulse(Transform tr, float factor, float totalDuration)
    {
        // If the transform or its GameObject has been destroyed, exit
        if (tr == null) return;
        Vector3 initial = tr.localScale;
        Vector3 target = initial * factor;
        float half = totalDuration * 0.5f;
        float elapsed = 0f;
        // scale up
        while (elapsed < half)
        {
            if (tr == null) return;
            tr.localScale = Vector3.Lerp(initial, target, elapsed / half);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        if (tr != null) tr.localScale = target;
        // scale down
        elapsed = 0f;
        while (elapsed < half)
        {
            if (tr == null) return;
            tr.localScale = Vector3.Lerp(target, initial, elapsed / half);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        if (tr != null) tr.localScale = initial;
    }
    
    // Re-enable nav buttons after disableDuration
    private async UniTask ReenableButtonsAsync()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(disableDuration));
        if (prevButton != null) prevButton.interactable = true;
        if (nextButton != null) nextButton.interactable = true;
    }

    // Creates a procedural white highlight behind slot icon
    private GameObject CreateHighlight(RectTransform slot, Sprite sprite)
    {
        var go = new GameObject("Highlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(slot, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        // Apply mask material to render white fill using alpha
        if (highlightMaterial != null)
        {
            img.material = highlightMaterial;
        }
        else
        {
            // Fallback: create material from shader
            var sh = Shader.Find("UI/AlphaMaskWhite");
            if (sh != null)
            {
                img.material = new Material(sh);
            }
            else
            {
                Debug.LogWarning("Highlight shader 'UI/AlphaMaskWhite' not found");
            }
        }
        // Fit sprite inside container*1.3, preserving aspect
        var rt = go.GetComponent<RectTransform>();
        // Apply scale multiplier to slot size
        float containerW = slot.rect.width * highlightScale;
        float containerH = slot.rect.height * highlightScale;
        float spriteAspect = sprite.rect.width / sprite.rect.height;
        float containerAspect = containerW / containerH;
        float width, height;
        if (spriteAspect > containerAspect)
        {
            width = containerW;
            height = containerW / spriteAspect;
        }
        else
        {
            height = containerH;
            width = containerH * spriteAspect;
        }
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        // Place behind other children
        go.transform.SetAsFirstSibling();
        return go;
    }
}
