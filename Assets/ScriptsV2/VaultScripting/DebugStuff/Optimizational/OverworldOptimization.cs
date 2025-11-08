using System.Collections;

using UnityEngine;
using System.Collections.Generic;

public class OverworldOptimization : MonoBehaviour
{
    [Header("Fog Settings")]
    public bool enableFog = true;
    public Color fogColor = Color.gray;
    public float fogStartDistance = 50f;
    public float fogEndDistance = 500f;
    public FogMode fogMode = FogMode.Linear;

    [Header("Chunk Settings")]
    public Transform playerCamera;
    public List<GameObject> terrainChunks = new List<GameObject>();
    public float chunkActivateDistance = 300f;

    [Header("LOD Settings")]
    public float lodDistanceMultiplier = 1f; // scale distances for LODs

    private void Awake()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        SetupFog();
    }

    private void Update()
    {
        if (playerCamera == null) return;

        UpdateChunks();
        UpdateLOD();
    }

    /// <summary>
    /// Configure global fog
    /// </summary>
    private void SetupFog()
    {
        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;
    }

    /// <summary>
    /// Activate or deactivate terrain chunks based on distance
    /// </summary>
    private void UpdateChunks()
    {
        foreach (var chunk in terrainChunks)
        {
            if (chunk == null) continue;
            float dist = Vector3.Distance(playerCamera.position, chunk.transform.position);
            bool shouldActivate = dist <= chunkActivateDistance;
            if (chunk.activeSelf != shouldActivate)
                chunk.SetActive(shouldActivate);
        }
    }

    /// <summary>
    /// Optional: Adjust LODGroup distances dynamically
    /// </summary>
  private void UpdateLOD()
{
    LODGroup[] lodGroups = FindObjectsOfType<LODGroup>();
    foreach (var lod in lodGroups)
    {
        LOD[] lods = lod.GetLODs();

        // Ensure descending order and clamp
        for (int i = 0; i < lods.Length; i++)
        {
            float newHeight = Mathf.Clamp(lods[i].screenRelativeTransitionHeight * lodDistanceMultiplier, 0f, 1f);
            // Ensure it is smaller than previous LOD (higher detail)
            if (i > 0 && newHeight >= lods[i - 1].screenRelativeTransitionHeight)
                newHeight = lods[i - 1].screenRelativeTransitionHeight * 0.99f;

            lods[i].screenRelativeTransitionHeight = newHeight;
        }

        lod.SetLODs(lods);
        lod.RecalculateBounds();
    }
}
}