using UnityEngine;
using UnityEngine.EventSystems;

public class StickerDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector3 _startPosition;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private bool _droppedSuccessfully;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Record original position in case we need to reset
        originalPosition = rectTransform.anchoredPosition;
        // Bring to front
        rectTransform.SetAsLastSibling();
        _startPosition = _rectTransform.position;
        _originalParent = _rectTransform.parent;
        _originalSiblingIndex = _rectTransform.GetSiblingIndex();
        _droppedSuccessfully = false;
        // Make the icon follow the mouse
        _rectTransform.SetParent(transform.root);
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        rectTransform.anchoredPosition = localPoint;
        _rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // You can reset to original position if drop not successful
        // rectTransform.anchoredPosition = originalPosition;
        _canvasGroup.blocksRaycasts = true;
        if (!_droppedSuccessfully)
        {
            _rectTransform.SetParent(_originalParent);
            _rectTransform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.position = _startPosition;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Notify this handler that the drop was successful and inventory icon should be removed.
    /// </summary>
    public void NotifyDropSuccess()
    {
        _droppedSuccessfully = true;
    }
}
