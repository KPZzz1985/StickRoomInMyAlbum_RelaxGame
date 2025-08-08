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

        // Only allow drop on matching placeholder zone
        if (transform.parent == null) return;
        string placeholderGroupName = transform.parent.name; // e.g. "PLACER_STK_15_eye"
        if (!placeholderGroupName.StartsWith("PLACER_STK_")) return;
        // Map placeholder group to sticker ID: replace prefix
        string placeholderStickerId = placeholderGroupName.Replace("PLACER_STK_", "STK_");
        if (data.stickerId != placeholderStickerId)
            return; // wrong sticker for this placeholder

        var sceneInit = FindObjectOfType<SceneInitializer>();
        if (sceneInit != null)
        {
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
