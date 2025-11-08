using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class PSXVertexSnapController : MonoBehaviour
{
    [Header("Snap Settings")]
    [Tooltip("How strong the snap is at the nearest distance (0 means camera near plane).")]
    public float snapClose = 0.02f;

    [Tooltip("How strong the snap is at the farthest distance (far clip plane).")]
    public float snapFar = 0.001f;

    [Tooltip("Whether to update snapping scale automatically every frame.")]
    public bool autoUpdate = true;

    private Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        UpdateSnapScale();
    }

    void Update()
    {
        if (autoUpdate)
            UpdateSnapScale();
    }

    void UpdateSnapScale()
    {
        if (!cam) return;

        // Calculate normalized distance-based scaling (far = smaller snap)
        Shader.SetGlobalFloat("_PSXSnapNear", snapClose);
        Shader.SetGlobalFloat("_PSXSnapFar", snapFar);
        Shader.SetGlobalFloat("_PSXNearPlane", cam.nearClipPlane);
        Shader.SetGlobalFloat("_PSXFarPlane", cam.farClipPlane);
    }
}
