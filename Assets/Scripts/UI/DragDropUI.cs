using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AutobattlerSample.UI
{
    public enum DragPayloadType
    {
        TeamUnit,
        CampUnit,
        CampItem
    }

    public sealed class UIDragPayload
    {
        public DragPayloadType Type;
        public object Value;
    }

    public static class DragDropState
    {
        public static UIDragPayload CurrentPayload;
    }

    public class UIDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private UIDragPayload _payload;
        private RectTransform _dragRoot;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private RectTransform _ghostRect;
        private CanvasGroup _ghostCanvasGroup;

        public void Init(UIDragPayload payload, RectTransform dragRoot)
        {
            _payload = payload;
            _dragRoot = dragRoot;
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_payload == null || _dragRoot == null)
                return;

            DragDropState.CurrentPayload = _payload;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.35f;

            var ghost = Instantiate(gameObject, _dragRoot);
            ghost.name = gameObject.name + "_DragGhost";
            _ghostRect = ghost.GetComponent<RectTransform>();
            _ghostRect.SetAsLastSibling();
            _ghostCanvasGroup = ghost.GetComponent<CanvasGroup>();
            if (_ghostCanvasGroup == null)
                _ghostCanvasGroup = ghost.AddComponent<CanvasGroup>();
            _ghostCanvasGroup.alpha = 0.7f;
            _ghostCanvasGroup.blocksRaycasts = false;

            foreach (var draggable in ghost.GetComponents<UIDraggable>())
                Destroy(draggable);
            foreach (var dropZone in ghost.GetComponents<UIDropZone>())
                Destroy(dropZone);
            foreach (var button in ghost.GetComponents<Button>())
                Destroy(button);

            MoveToPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_payload == null || _dragRoot == null)
                return;

            MoveToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            if (_ghostRect != null)
                Destroy(_ghostRect.gameObject);
            _ghostRect = null;
            _ghostCanvasGroup = null;

            DragDropState.CurrentPayload = null;
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _dragRoot, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                if (_ghostRect != null)
                {
                    _ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
                    _ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
                    _ghostRect.pivot = new Vector2(0.5f, 0.5f);
                    _ghostRect.anchoredPosition = localPoint;
                }
            }
        }
    }

    public class UIDropZone : MonoBehaviour, IDropHandler
    {
        private Func<UIDragPayload, bool> _canAccept;
        private Action<UIDragPayload> _onDrop;

        public void Init(Func<UIDragPayload, bool> canAccept, Action<UIDragPayload> onDrop)
        {
            _canAccept = canAccept;
            _onDrop = onDrop;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var payload = DragDropState.CurrentPayload;
            if (payload == null || _onDrop == null)
                return;

            if (_canAccept != null && !_canAccept(payload))
                return;

            _onDrop(payload);
        }
    }
}
