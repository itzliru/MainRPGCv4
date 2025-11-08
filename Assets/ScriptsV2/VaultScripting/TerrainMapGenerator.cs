using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TerrainMapGenerator : MonoBehaviour
{
    [Header("Terrain & Camera Settings")]
    public Terrain terrain;
    public Camera mapCamera;
    public bool includeObjects = true; // Render objects on terrain

    [Header("Output Settings")]
    public string savePath = "Assets/TerrainMap.png";
    public int finalTextureSize = 512;    // Base stitched size
    public int upscaleResolution = 2048;  // Output resolution after upscaling

    private void Start()
    {
        if (terrain == null || mapCamera == null)
        {
            Debug.LogError("Assign Terrain and Camera before running.");
            return;
        }

        GenerateMap();
    }

    void GenerateMap()
    {
        int terrainWidth = Mathf.RoundToInt(terrain.terrainData.size.x);
        int terrainLength = Mathf.RoundToInt(terrain.terrainData.size.z);

        int chunksX = 2;
        int chunksZ = 2;

        int chunkPixelSize = finalTextureSize / 2;

        Texture2D finalTexture = new Texture2D(finalTextureSize, finalTextureSize, TextureFormat.RGB24, false);

        // Backup camera
        bool originalOrtho = mapCamera.orthographic;
        float originalSize = mapCamera.orthographicSize;
        Vector3 originalPos = mapCamera.transform.position;
        LayerMask originalCulling = mapCamera.cullingMask;

        mapCamera.orthographic = true;
        mapCamera.farClipPlane = 10000;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = Color.black;

        if (!includeObjects)
            mapCamera.cullingMask = LayerMask.GetMask("Terrain");

        RenderTexture rt = new RenderTexture(chunkPixelSize, chunkPixelSize, 24);
        mapCamera.targetTexture = rt;

        for (int x = 0; x < chunksX; x++)
        {
            for (int z = 0; z < chunksZ; z++)
            {
                float chunkCenterX = (terrainWidth / 2f) * (x * 2 + 1) / 2f;
                float chunkCenterZ = (terrainLength / 2f) * (z * 2 + 1) / 2f;

                float maxHeight = terrain.terrainData.size.y + 10f;
                mapCamera.transform.position = new Vector3(chunkCenterX, maxHeight, chunkCenterZ);
                mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                mapCamera.orthographicSize = terrainWidth / 4f;

                mapCamera.Render();

                RenderTexture.active = rt;
                Texture2D chunkTex = new Texture2D(chunkPixelSize, chunkPixelSize, TextureFormat.RGB24, false);
                chunkTex.ReadPixels(new Rect(0, 0, chunkPixelSize, chunkPixelSize), 0, 0);
                chunkTex.Apply();

                int destX = x * chunkPixelSize;
                int destY = z * chunkPixelSize;
                finalTexture.SetPixels(destX, destY, chunkPixelSize, chunkPixelSize, chunkTex.GetPixels());
            }
        }

        finalTexture.Apply();

        // Upscale to higher resolution
        Texture2D upscaledTexture = new Texture2D(upscaleResolution, upscaleResolution, TextureFormat.RGB24, false);
        for (int y = 0; y < upscaleResolution; y++)
        {
            for (int x = 0; x < upscaleResolution; x++)
            {
                float u = x / (float)upscaleResolution;
                float v = y / (float)upscaleResolution;
                upscaledTexture.SetPixel(x, y, finalTexture.GetPixelBilinear(u, v));
            }
        }
        upscaledTexture.Apply();

        // Save texture
        byte[] bytes = upscaledTexture.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);
        Debug.Log($"Terrain map saved to: {savePath}");

        // Restore camera
        mapCamera.orthographic = originalOrtho;
        mapCamera.orthographicSize = originalSize;
        mapCamera.transform.position = originalPos;
        mapCamera.cullingMask = originalCulling;

        mapCamera.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
    }
}
