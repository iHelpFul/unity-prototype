using UnityEngine;

public class VfxFollowTarget : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;

    public void Initialize(Transform followTarget, Vector3 followOffset)
    {
        target = followTarget;
        offset = followOffset;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
    }
}
