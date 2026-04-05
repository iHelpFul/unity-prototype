using UnityEngine;

public class PlayerHitFlashController : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;

    private MaterialPropertyBlock block;

    private float flashTimer;
    private float flashDuration;

    private static readonly int ColorID = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        block = new MaterialPropertyBlock();
    }

    public void PlayFlash(float duration)
    {
        if (duration <= 0f)
        {
            flashDuration = 0f;
            flashTimer = 0f;
            ResetColor();
            return;
        }

        flashDuration = duration;
        flashTimer = duration;
    }

    private void Update()
    {
        if (flashTimer <= 0f)
            return;

        flashTimer -= Time.deltaTime;

        float t = flashTimer / flashDuration;

        // Maple style:
        // מתחיל כהה → מהבהב → נרגע
        float intensity;

        if (t > 0.95f)
            intensity = 0.2f; // כהה חזק
        else
            intensity = Mathf.PingPong(Time.time * 20f, 1f) > 0.5f ? 0.3f : 1f;

        ApplyColor(intensity);

        if (flashTimer <= 0f)
            ResetColor();
    }

    private void ApplyColor(float multiplier)
    {
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled)
                continue;

            block.Clear();
            block.SetColor(ColorID, Color.white * multiplier);
            r.SetPropertyBlock(block);
        }
    }

    private void ResetColor()
    {
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled)
                continue;

            block.Clear();
            r.SetPropertyBlock(block);
        }
    }
}
