using UnityEngine;
using UnityEngine.EventSystems;

public class PlaceholderArea : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragObj = eventData.pointerDrag;
        if (dragObj == null) return;
        var data = dragObj.GetComponent<StickerDragData>();
        if (data == null || string.IsNullOrEmpty(data.stickerId)) return;

        // Convert pointer position to world point
        Vector3 screenPoint = eventData.position;
        Vector3 worldPoint3 = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane));
        Vector2 worldPoint = new Vector2(worldPoint3.x, worldPoint3.y);

        // Check all 2D colliders at this point
        var hits = Physics2D.OverlapPointAll(worldPoint);
        bool placed = false;
        foreach (var hit in hits)
        {
            var ph = hit.GetComponent<PlaceholderArea>();
            if (ph == null) continue;
            // Determine placeholderStickerId from ph.gameObject.name
            string rawName = ph.gameObject.name.Trim();
            const string prefix = "PLACER_";
            if (!rawName.StartsWith(prefix)) continue;
            string rawId = rawName.Substring(prefix.Length);
            string placeholderStickerId = rawId;
            int lastUnd = rawId.LastIndexOf('_');
            if (lastUnd > 0)
            {
                string suffix = rawId.Substring(lastUnd + 1);
                bool isNumeric = true;
                foreach (char c in suffix) if (!char.IsDigit(c)) { isNumeric = false; break; }
                if (isNumeric)
                    placeholderStickerId = rawId.Substring(0, lastUnd);
            }
            // Match stickerId
            if (data.stickerId != placeholderStickerId) continue;
            // Found matching placeholder - place
            var sceneInit = FindObjectOfType<SceneInitializer>();
            if (sceneInit != null)
                sceneInit.PlaceSticker(data.stickerId, ph.transform);
            placed = true;
            break;
        }
        if (!placed) return;

        // Notify UI icon it was dropped successfully
        var dragHandler = dragObj.GetComponent<StickerDragHandler>();
        if (dragHandler != null)
            dragHandler.NotifyDropSuccess();

        // Optionally disable all placeholders that were hit?
        // ph.transform.gameObject.SetActive(false); // already done in PlaceSticker for this ph
    }
}
