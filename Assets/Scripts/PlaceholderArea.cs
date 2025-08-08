using UnityEngine;
using UnityEngine.EventSystems;

public class PlaceholderArea : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragObj = eventData.pointerDrag;
        if (dragObj == null) return;
        var data = dragObj.GetComponent<StickerDragData>();
        if (data == null) return;

        var stickerPrefab = data.stickerPrefab;
        var sceneInit = FindObjectOfType<SceneInitializer>();
        if (sceneInit != null && stickerPrefab != null)
            sceneInit.PlaceSticker(stickerPrefab, transform.position);
        
        // Optionally disable placeholder
        gameObject.SetActive(false);
    }
}
