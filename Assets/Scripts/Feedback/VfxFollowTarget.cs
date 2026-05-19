using UnityEngine;

public class VfxFollowTarget : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private bool useFacingYaw;
    private float rightFacingYaw;
    private float leftFacingYaw;

    public void Initialize(
        Transform followTarget,
        Vector3 followOffset,
        bool shouldUseFacingYaw,
        float rightYaw,
        float leftYaw)
    {
        target = followTarget;
        offset = followOffset;
        useFacingYaw = shouldUseFacingYaw;
        rightFacingYaw = rightYaw;
        leftFacingYaw = leftYaw;

        if (target != null && useFacingYaw)
            transform.rotation = ResolveFacingRotation(target);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = target.position + ResolveFacingOffset(target);

        if (useFacingYaw)
            transform.rotation = ResolveFacingRotation(target);
    }

    private Vector3 ResolveFacingOffset(Transform followTarget)
    {
        if (followTarget == null || !useFacingYaw)
            return offset;

        Vector3 resolvedOffset = offset;
        resolvedOffset.x = followTarget.forward.x >= 0f
            ? Mathf.Abs(resolvedOffset.x)
            : -Mathf.Abs(resolvedOffset.x);
        return resolvedOffset;
    }

    private Quaternion ResolveFacingRotation(Transform followTarget)
    {
        if (followTarget == null)
            return transform.rotation;
            
        float yaw = followTarget.forward.x >= 0f
            ? rightFacingYaw
            : leftFacingYaw;

        return Quaternion.Euler(0f, yaw, 0f);
    }
}
