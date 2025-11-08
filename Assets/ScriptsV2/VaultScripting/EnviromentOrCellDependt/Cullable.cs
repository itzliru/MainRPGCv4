using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cullable : MonoBehaviour
{
    private Renderer[] renderers;
    public bool IsVisible { get; private set; } = true;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void SetVisible(bool state)
    {
        IsVisible = state;

        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = state;
        }
    }
}
