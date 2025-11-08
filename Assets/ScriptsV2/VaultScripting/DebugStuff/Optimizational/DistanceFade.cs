using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldOcclusionCulling : MonoBehaviour
{
    [Header("Culling Settings")]
    public float checkInterval = 0.1f;   // Seconds between checks
    public float maxDistance = 100f;     // Cull objects further than this
    [Range(1f, 179f)] public float fieldOfView = 90f; // Camera FOV angle

    private Transform cam;
    private float nextCheckTime;
    private List<Cullable> cullables = new List<Cullable>();

    void Awake()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
            Debug.LogError("[OverworldOcclusionCulling] No main camera found!");
        
        RegisterAllCullables();
    }

    void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            UpdateCulling();
        }
    }

    void RegisterAllCullables()
    {
        cullables.Clear();
        Cullable[] found = GameObject.FindObjectsOfType<Cullable>();
        cullables.AddRange(found);
    }

    void UpdateCulling()
    {
        if (cam == null) return;

        Vector3 camPos = cam.position;
        Vector3 camForward = cam.forward;

        float halfFOV = fieldOfView * 0.5f;
        float cosFOV = Mathf.Cos(halfFOV * Mathf.Deg2Rad);

        foreach (Cullable c in cullables)
        {
            if (c == null) continue;

            Vector3 toObject = c.transform.position - camPos;
            float distance = toObject.magnitude;

            // Ignore extremely close or zero-length distances
            if (distance <= 0.01f) continue;

            Vector3 dirToObject = toObject.normalized;

            // Dot = cosine of angle between camera forward and object direction
            float dot = Vector3.Dot(camForward, dirToObject);

            // In front and within the FOV cone
            bool inFOV = dot > cosFOV;
            bool withinDistance = distance <= maxDistance;

            bool shouldBeVisible = inFOV && withinDistance;

            if (c.IsVisible != shouldBeVisible)
                c.SetVisible(shouldBeVisible);
        }
    }
}
