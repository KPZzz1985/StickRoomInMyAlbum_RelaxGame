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
        var sceneInit = FindObjectOfType<SceneInitializer>();
        if (sceneInit != null)
        {
            // Activate existing sticker in stash by ID
            sceneInit.PlaceSticker(data.stickerId, transform);
        }

        // Notify UI icon it was dropped successfully so it can be destroyed
        var dragHandler = dragObj.GetComponent<StickerDragHandler>();
        if (dragHandler != null)
            dragHandler.NotifyDropSuccess();

        // Optionally disable placeholder
        gameObject.SetActive(false);
    }
}
