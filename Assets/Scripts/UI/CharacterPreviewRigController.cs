using UnityEngine;

public class CharacterPreviewRigController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform zoomTarget;

    [Header("Rotation")]
    [SerializeField] private float dragRotationSpeed = 0.35f;
    [SerializeField] private float buttonRotationStep = 25f;
    [SerializeField] private float rotationSmoothTime = 0.08f;

    [Header("Zoom")]
    [SerializeField] private float scrollZoomSpeed = 0.4f;
    [SerializeField] private float buttonZoomStep = 0.3f;
    [SerializeField] private float minZoomDistance = 1.4f;
    [SerializeField] private float maxZoomDistance = 3.8f;
    [SerializeField] private float zoomSmoothTime = 0.08f;

    private float targetYaw;
    private float currentYawVelocity;
    private Vector3 zoomDirection = Vector3.back;
    private float targetZoomDistance;
    private float currentZoomVelocity;
    private float defaultYaw;
    private float defaultZoomDistance;

    private void Awake()
    {
        if (modelRoot == null)
            modelRoot = transform;

        if (zoomTarget != null)
        {
            Vector3 localPosition = zoomTarget.localPosition;
            float magnitude = localPosition.magnitude;
            zoomDirection = magnitude > 0.0001f ? localPosition / magnitude : Vector3.back;
            targetZoomDistance = Mathf.Clamp(magnitude, minZoomDistance, maxZoomDistance);
            defaultZoomDistance = targetZoomDistance;
        }

        targetYaw = GetCurrentYaw();
        defaultYaw = targetYaw;
    }

    private void OnEnable()
    {
        targetYaw = GetCurrentYaw();
        if (zoomTarget != null)
            targetZoomDistance = Mathf.Clamp(zoomTarget.localPosition.magnitude, minZoomDistance, maxZoomDistance);
    }

    private void Update()
    {
        UpdateRotation();
        UpdateZoom();
    }

    public void RotateByDrag(float horizontalDelta)
    {
        targetYaw -= horizontalDelta * dragRotationSpeed;
    }

    public void RotateLeft()
    {
        targetYaw += buttonRotationStep;
    }

    public void RotateRight()
    {
        targetYaw -= buttonRotationStep;
    }

    public void ZoomByScroll(float scrollDelta)
    {
        targetZoomDistance = Mathf.Clamp(
            targetZoomDistance - (scrollDelta * scrollZoomSpeed),
            minZoomDistance,
            maxZoomDistance);
    }

    public void ZoomIn()
    {
        targetZoomDistance = Mathf.Clamp(targetZoomDistance - buttonZoomStep, minZoomDistance, maxZoomDistance);
    }

    public void ZoomOut()
    {
        targetZoomDistance = Mathf.Clamp(targetZoomDistance + buttonZoomStep, minZoomDistance, maxZoomDistance);
    }

    public void ResetView()
    {
        targetYaw = defaultYaw;
        targetZoomDistance = defaultZoomDistance;
    }

    private void UpdateRotation()
    {
        if (modelRoot == null)
            return;

        float currentYaw = GetCurrentYaw();
        float nextYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref currentYawVelocity, rotationSmoothTime);
        Vector3 localEuler = modelRoot.localEulerAngles;
        localEuler.y = nextYaw;
        modelRoot.localEulerAngles = localEuler;
    }

    private void UpdateZoom()
    {
        if (zoomTarget == null)
            return;

        float currentDistance = zoomTarget.localPosition.magnitude;
        float nextDistance = Mathf.SmoothDamp(currentDistance, targetZoomDistance, ref currentZoomVelocity, zoomSmoothTime);
        zoomTarget.localPosition = zoomDirection * nextDistance;
    }

    private float GetCurrentYaw()
    {
        if (modelRoot == null)
            return 0f;

        return modelRoot.localEulerAngles.y;
    }
}
