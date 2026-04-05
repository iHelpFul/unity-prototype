using Unity.Cinemachine;
using UnityEngine;

public static class MultiplayerPrototypeCameraBinder
{
    public static void BindTo(Transform target)
    {
        if (target == null)
            return;

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int index = 0; index < cameras.Length; index++)
        {
            CinemachineCamera camera = cameras[index];
            if (camera == null)
                continue;

            camera.Follow = target;
        }
    }

    public static void ClearBindings(Transform target)
    {
        if (target == null)
            return;

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int index = 0; index < cameras.Length; index++)
        {
            CinemachineCamera camera = cameras[index];
            if (camera != null && camera.Follow == target)
                camera.Follow = null;
        }
    }
}
