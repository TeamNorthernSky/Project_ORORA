using UnityEngine;
using UnityEngine.EventSystems;

public class PanelDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private RectTransform target;
    private Vector2 dragOffset;
    private System.Action<Vector2> onDragged;

    public void SetTarget(RectTransform targetRect, System.Action<Vector2> onDraggedCallback = null)
    {
        target = targetRect;
        onDragged = onDraggedCallback;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 mousePos
        );
        dragOffset = (Vector2)target.localPosition - mousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (target == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 mousePos
        );
        target.localPosition = mousePos + dragOffset;
        onDragged?.Invoke(target.anchoredPosition);
    }
}
