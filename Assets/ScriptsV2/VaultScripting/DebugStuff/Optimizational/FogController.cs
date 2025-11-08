using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogController : MonoBehaviour
{
    [Header("Fog Settings")]
    public Color fogColor = Color.gray;
    public float fogStartDistance = 50f;
    public float fogEndDistance = 300f;


    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear; // Linear, Exponential, ExponentialSquared
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;
    }
}