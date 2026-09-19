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
            // Explicit Unity null check: a destroyed UnityEngine.Object is not
            // managed-null, so null-conditional can still invoke it.
            if (controller != null)
                controller.NotifyMapDragged();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (controller != null)
                controller.SelectNearestToViewport();
        }

#if UNITY_EDITOR
        public void EditorConfigure(WorldMapSceneController mapController)
        {
            controller = mapController;
        }
#endif
    }
}
