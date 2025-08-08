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
        if (leftInst != null) Destroy(leftInst);
        if (centerInst != null) Destroy(centerInst);
        if (rightInst != null) Destroy(rightInst);
    }

    private void UpdateSlots()
    {
        ClearSlots();
        int count = stash.Count;
        if (count == 0) return;
        // Left slot: only if previous exists
        if (currentIndex > 0)
        {
            int leftIdx = currentIndex - 1;
            leftInst = Instantiate(iconPrefab, leftSlot, false);
            SetupSlot(leftInst, stash[leftIdx], draggable: false);
        }
        // Center slot: always
        centerInst = Instantiate(iconPrefab, centerSlot, false);
        SetupSlot(centerInst, stash[currentIndex], draggable: true);
        // Right slot: only if next exists
        if (currentIndex + 1 < count)
        {
            int rightIdx = currentIndex + 1;
            rightInst = Instantiate(iconPrefab, rightSlot, false);
            SetupSlot(rightInst, stash[rightIdx], draggable: false);
        }
    }

    private void SetupSlot(GameObject inst, Entry e, bool draggable)
    {
        var img = inst.GetComponent<Image>();
        if (img != null) img.sprite = e.sprite;
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
            AnimateWaveNext().Forget();
        }
    }

    /// <summary>Call after a sticker is placed to remove it and refill carousel.</summary>
    public void UseCurrent()
    {
        if (stash.Count == 0) return;
        stash.RemoveAt(currentIndex);
        if (stash.Count == 0) { ClearSlots(); return; }
        if (currentIndex >= stash.Count) currentIndex = stash.Count - 1;
        UpdateSlots();
    }

    // Wave animation when pressing Next: right->center->left
    private async UniTaskVoid AnimateWaveNext()
    {
        if (rightInst != null)
            await ScalePulse(rightInst.transform, 2f, 0.075f);
        if (centerInst != null)
            await ScalePulse(centerInst.transform, 1.5f, 0.075f);
        if (leftInst != null)
            await ScalePulse(leftInst.transform, 1.25f, 0.075f);
    }

    // Wave animation when pressing Prev: left->center->right
    private async UniTaskVoid AnimateWavePrev()
    {
        if (leftInst != null)
            await ScalePulse(leftInst.transform, 2f, 0.075f);
        if (centerInst != null)
            await ScalePulse(centerInst.transform, 1.5f, 0.075f);
        if (rightInst != null)
            await ScalePulse(rightInst.transform, 1.25f, 0.075f);
    }

    // Scale pulse: scale to factor and back over totalDuration (half up and half down)
    private async UniTask ScalePulse(Transform tr, float factor, float totalDuration)
    {
        Vector3 initial = tr.localScale;
        Vector3 target = initial * factor;
        float half = totalDuration * 0.5f;
        float elapsed = 0f;
        // scale up
        while (elapsed < half)
        {
            tr.localScale = Vector3.Lerp(initial, target, elapsed / half);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        tr.localScale = target;
        // scale down
        elapsed = 0f;
        while (elapsed < half)
        {
            tr.localScale = Vector3.Lerp(target, initial, elapsed / half);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        tr.localScale = initial;
    }
}
