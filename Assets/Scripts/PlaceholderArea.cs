using UnityEngine;
using UnityEngine.EventSystems;

public class PlaceholderArea : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragObj = eventData.pointerDrag;
        if (dragObj == null) return;
        var data = dragObj.GetComponent<StickerDragData>();
        if (data == null || string.IsNullOrEmpty(data.stickerId))
        {
            Debug.Log("PlaceholderArea.OnDrop: no StickerDragData or empty stickerId");
            return;
        }

        string rawName = gameObject.name;
        Debug.Log($"PlaceholderArea.OnDrop: Attempting to drop sticker '{data.stickerId}' on placeholder '{rawName}'");
        // Trim any whitespace in placeholder name
        string placeholderName = rawName.Trim(); // e.g. "PLACER_STK_15_eye_1"
        const string prefix = "PLACER_";
        if (!placeholderName.StartsWith(prefix))
        {
            Debug.LogWarning($"PlaceholderArea.OnDrop: Invalid placeholder name '{placeholderName}'");
            return;
        }
        // Raw ID part after "PLACER_"
        string rawId = placeholderName.Substring(prefix.Length);
        // If rawId ends with numeric suffix like "_1", trim only that
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
        Debug.Log($"PlaceholderArea.OnDrop: resolved placeholderStickerId = '{placeholderStickerId}'");
        if (data.stickerId != placeholderStickerId)
        {
            Debug.LogWarning($"PlaceholderArea.OnDrop: stickerId '{data.stickerId}' does not match placeholderStickerId '{placeholderStickerId}'");
            return; // wrong sticker for this placeholder
        }

        var sceneInit = FindObjectOfType<SceneInitializer>();
        if (sceneInit != null)
        {
            Debug.Log($"PlaceholderArea.OnDrop: matched, invoking PlaceSticker for '{data.stickerId}'");
            sceneInit.PlaceSticker(data.stickerId, transform);
        }

        // Notify UI icon it was dropped successfully so it can be destroyed
        var dragHandler = dragObj.GetComponent<StickerDragHandler>();
        if (dragHandler != null)
            dragHandler.NotifyDropSuccess();

        // Disable placeholder after successful drop
        gameObject.SetActive(false);
    }
}
