using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Manages the inventory ScrollRect to scale icons in a bead-like fashion,
/// snap to nearest on release, and enable dragging only on the center icon.
/// Attach this to the ScrollRect GameObject.
/// </summary>
public class InventoryScrollManager : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [Tooltip("Scale of the centered icon.")]
    [SerializeField] private float maxScale = 1.2f;
    [Tooltip("Scale of the icons at the very edges.")]
    [SerializeField] private float minScale = 0.8f;
    [Tooltip("Curve to modulate scale based on normalized distance from center [0..1].")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<RectTransform> icons = new List<RectTransform>();
    private bool isDragging = false;
    private float itemWidth;
    private float itemSpacing;

    private void Start()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (content == null && scrollRect != null) content = scrollRect.content;
        scrollRect.onValueChanged.AddListener(_ => OnScroll());

        // Delay initial caching and measure item size for infinite scroll
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        // Wait one frame for SceneInitializer to populate inventory
        yield return null;
        // Cache icon transforms after inventory is ready
        icons.Clear();
        foreach (Transform child in content)
        {
            if (child is RectTransform rt)
                icons.Add(rt);
        }
        // Measure item width and spacing
        if (icons.Count > 0)
        {
            itemWidth = icons[0].rect.width;
            var layout = content.GetComponent<HorizontalLayoutGroup>();
            itemSpacing = layout != null ? layout.spacing : 0f;
        }
        UpdateIconScales();
    }

    private void OnScroll()
    {
        isDragging = true;
        WrapContent();
        UpdateIconScales();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;
        SnapToNearest();
    }

    /// <summary>
    /// Wrap icons to create infinite scrolling effect.
    /// </summary>
    private void WrapContent()
    {
        float threshold = itemWidth + itemSpacing;
        Vector2 pos = content.anchoredPosition;
        // Scrolled right beyond threshold
        if (pos.x <= -threshold)
        {
            content.anchoredPosition = new Vector2(pos.x + threshold, pos.y);
            var first = icons[0];
            first.SetSiblingIndex(icons.Count - 1);
            icons.RemoveAt(0);
            icons.Add(first);
        }
        // Scrolled left beyond threshold
        else if (pos.x >= threshold)
        {
            content.anchoredPosition = new Vector2(pos.x - threshold, pos.y);
            var last = icons[icons.Count - 1];
            last.SetSiblingIndex(0);
            icons.RemoveAt(icons.Count - 1);
            icons.Insert(0, last);
        }
    }

    private void UpdateIconScales()
    {
        // Dynamic recache if number of icons changed
        if (icons.Count != content.childCount)
        {
            icons.Clear();
            foreach (Transform child in content)
            {
                if (child is RectTransform rt)
                    icons.Add(rt);
            }
        }
        // World-space center of viewport
        Vector3 viewportCenter = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);
        float halfWidth = scrollRect.viewport.rect.width * 0.5f;
        foreach (RectTransform icon in icons)
        {
            // Icon world center
            Vector3 iconWorld = icon.TransformPoint(icon.rect.center);
            float distance = Vector2.Distance(iconWorld, viewportCenter);
            // Normalize distance (0 = center, 1 = halfWidth)
            float t = Mathf.Clamp01(distance / halfWidth);
            float scale = Mathf.Lerp(maxScale, minScale, scaleCurve.Evaluate(t));
            icon.localScale = new Vector3(scale, scale, 1f);

            // Enable drag only on icons that are effectively centered
            var dragHandler = icon.GetComponent<StickerDragHandler>();
            if (dragHandler != null)
                dragHandler.enabled = (scale > (maxScale + minScale) * 0.5f);
        }
    }

    private void SnapToNearest()
    {
        // Find nearest icon
        int nearestIndex = 0;
        float minDist = float.MaxValue;
        Vector3 viewportCenter = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);
        for (int i = 0; i < icons.Count; i++)
        {
            Vector3 worldPos = icons[i].TransformPoint(icons[i].rect.center);
            float dist = Vector2.Distance(worldPos, viewportCenter);
            if (dist < minDist)
            {
                minDist = dist;
                nearestIndex = i;
            }
        }

        // Compute target content anchoredPosition so that nearest icon is centered
        float viewportWidth = scrollRect.viewport.rect.width;
        Vector2 iconLocal = content.InverseTransformPoint(icons[nearestIndex].TransformPoint(icons[nearestIndex].rect.center));
        float targetX = -iconLocal.x;
        float contentWidth = content.rect.width;
        // Clamp so content stays within bounds
        float minX = -(contentWidth - viewportWidth);
        float clampedX = Mathf.Clamp(targetX, minX, 0f);

        // Animate content to new position
        StopAllCoroutines();
        StartCoroutine(SmoothMove(content.anchoredPosition, new Vector2(clampedX, content.anchoredPosition.y), 0.3f));
    }

    private System.Collections.IEnumerator SmoothMove(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            content.anchoredPosition = Vector2.Lerp(from, to, t);
            UpdateIconScales();
            yield return null;
        }
        content.anchoredPosition = to;
        UpdateIconScales();
    }
}
