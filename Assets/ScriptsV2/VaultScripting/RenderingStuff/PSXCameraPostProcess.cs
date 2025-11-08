using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class PSXCameraPostProcessOptimized : MonoBehaviour
{
    [Header("Retro Resolution Settings (Puso)")]
    public int retroWidth = 320;
    public int retroHeight = 240;
    public bool maintainAspectRatio = true;

    [Header("Pixelation & Wobble Settings")]
    public bool enableWobble = true;
    [Range(0f, 1f)] public float wobbleAmount = 0.5f;
    [Range(1, 16)] public int pixelation = 2;

    [Header("Fog & Distance Tint Settings")]
    public bool enableFog = true;
    public Color fogColor = Color.gray;
    [Range(0.001f, 0.1f)] public float fogDensity = 0.02f;

    public bool enableDistanceDarken = true;
    [Range(0f, 1f)] public float darkenStrength = 0.3f;

    [Header("Camera Settings")]
    public float farClip = 200f;

    [Header("Post FX Performance")]
    public bool disableAA = true;
    public bool enablePointFiltering = true;

    [Header("Shader Setup")]
    public Shader retroShader;

    private Material retroMaterial;
    private Camera cam;
    private RenderTexture rt;

    private int currentWidth = 0;
    private int currentHeight = 0;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.allowDynamicResolution = false;

        if (disableAA)
            QualitySettings.antiAliasing = 0;

        if (retroShader != null && retroShader.isSupported)
            retroMaterial = new Material(retroShader);

        if (enablePointFiltering)
        {
            foreach (var tex in Resources.FindObjectsOfTypeAll<Texture>())
                tex.filterMode = FilterMode.Point;
        }
    }

    private void OnDisable()
    {
        if (rt != null)
        {
            rt.Release();
            rt = null;
        }

        if (cam != null)
            cam.targetTexture = null;
    }

    private void Update()
    {
        cam.farClipPlane = farClip;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (retroMaterial == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // Update shader properties
        retroMaterial.SetFloat("_Pixelation", pixelation);
        retroMaterial.SetFloat("_WobbleAmount", wobbleAmount);
        retroMaterial.SetFloat("_EnableWobble", enableWobble ? 1f : 0f);
        retroMaterial.SetColor("_FogColor", fogColor);
        retroMaterial.SetFloat("_FogDensity", enableFog ? fogDensity : 0f);
        retroMaterial.SetFloat("_EnableDarken", enableDistanceDarken ? 1f : 0f);
        retroMaterial.SetFloat("_DarkenStrength", darkenStrength);

        // Retro resolution with aspect ratio
        int targetWidth = retroWidth;
        int targetHeight = retroHeight;
        if (maintainAspectRatio)
        {
            float aspect = (float)Screen.width / Screen.height;
            targetHeight = Mathf.RoundToInt(targetWidth / aspect);
        }

        // Only recreate RT if resolution changed
        if (rt == null || currentWidth != targetWidth || currentHeight != targetHeight)
        {
            if (rt != null) rt.Release();
            rt = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            rt.useMipMap = false;
            currentWidth = targetWidth;
            currentHeight = targetHeight;
        }

        // Single-pass blit: src -> RT -> dest with shader
        Graphics.Blit(src, rt, retroMaterial);
        Graphics.Blit(rt, dest);
    }
}
