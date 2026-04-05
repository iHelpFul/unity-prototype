using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterPreviewInteractionSurface : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField] private CharacterPreviewRigController previewRig;
    [SerializeField] private bool enableDragRotation = true;
    [SerializeField] private bool enableScrollZoom = true;

    public void OnDrag(PointerEventData eventData)
    {
        if (!enableDragRotation || previewRig == null)
            return;

        previewRig.RotateByDrag(eventData.delta.x);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!enableScrollZoom || previewRig == null)
            return;

        previewRig.ZoomByScroll(eventData.scrollDelta.y);
    }

    public void RotateLeft()
    {
        previewRig?.RotateLeft();
    }

    public void RotateRight()
    {
        previewRig?.RotateRight();
    }

    public void ZoomIn()
    {
        previewRig?.ZoomIn();
    }

    public void ZoomOut()
    {
        previewRig?.ZoomOut();
    }

    public void ResetView()
    {
        previewRig?.ResetView();
    }
}
