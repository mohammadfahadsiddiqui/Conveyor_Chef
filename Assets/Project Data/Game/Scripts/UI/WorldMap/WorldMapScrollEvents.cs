using UnityEngine;
using UnityEngine.EventSystems;

namespace Watermelon.BusStop
{
    [DisallowMultipleComponent]
    public sealed class WorldMapScrollEvents : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private WorldMapSceneController controller;

        public void OnBeginDrag(PointerEventData eventData)
        {
            controller?.NotifyMapDragged();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            controller?.SelectNearestToViewport();
        }

#if UNITY_EDITOR
        public void EditorConfigure(WorldMapSceneController mapController)
        {
            controller = mapController;
        }
#endif
    }
}
