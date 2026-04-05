using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class MultiplayerPrototypeNameplate : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 fallbackWorldOffset = new Vector3(0f, 2.6f, 0f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 3f;

    private TextMeshPro label;
    private Transform labelTransform;
    private Renderer[] cachedRenderers;
    private string currentText = string.Empty;

    private void Awake()
    {
        if (target == null)
            target = transform;

        EnsureLabel();
        RefreshRendererCache();
    }

    private void LateUpdate()
    {
        if (labelTransform == null)
            return;

        UpdateWorldPosition();
        FaceCamera();
    }

    public void SetText(string value)
    {
        EnsureLabel();

        string nextValue = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (currentText == nextValue)
            return;

        currentText = nextValue;
        label.text = currentText;
        label.gameObject.SetActive(!string.IsNullOrWhiteSpace(currentText));
    }

    public void SetTarget(Transform nextTarget)
    {
        target = nextTarget != null ? nextTarget : transform;
        RefreshRendererCache();
        UpdateWorldPosition();
    }

    private void EnsureLabel()
    {
        if (label != null)
            return;

        GameObject labelObject = new GameObject("Prototype Nameplate");
        labelObject.transform.SetParent(transform, false);
        labelObject.layer = gameObject.layer;

        label = labelObject.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;

        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.color = textColor;
        label.text = string.Empty;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.outlineWidth = 0.2f;
        label.outlineColor = Color.black;

        labelTransform = labelObject.transform;
        labelObject.SetActive(false);
    }

    private void RefreshRendererCache()
    {
        cachedRenderers = target != null
            ? target.GetComponentsInChildren<Renderer>(true)
            : System.Array.Empty<Renderer>();
    }

    private void UpdateWorldPosition()
    {
        Vector3 labelPosition = target != null ? target.position + fallbackWorldOffset : transform.position + fallbackWorldOffset;
        float highestY = float.MinValue;

        for (int index = 0; index < cachedRenderers.Length; index++)
        {
            Renderer renderer = cachedRenderers[index];
            if (renderer == null || renderer.transform == labelTransform)
                continue;

            highestY = Mathf.Max(highestY, renderer.bounds.max.y);
        }

        if (highestY > float.MinValue)
            labelPosition.y = highestY + 0.25f;

        labelTransform.position = labelPosition;
    }

    private void FaceCamera()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
            return;

        Transform cameraTransform = activeCamera.transform;
        Vector3 forward = cameraTransform.position - labelTransform.position;
        if (forward.sqrMagnitude <= 0.0001f)
            return;

        labelTransform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }
}
