using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class PSXWaterScroll : MonoBehaviour
{
    [Header("Scroll Settings")]
    public float scrollSpeedX = 0.05f;
    public float scrollSpeedY = 0.02f;
    public bool useWorldSpaceMotion = false;

    private Renderer rend;
    private Vector2 offset;

    void Start()
    {
        rend = GetComponent<Renderer>();
        offset = Vector2.zero;
    }

    void Update()
    {
        if (rend == null) return;

        float delta = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        offset.x = delta * scrollSpeedX;
        offset.y = delta * scrollSpeedY;

        if (useWorldSpaceMotion)
        {
            // make the water appear to flow in world-space (for large bodies)
            Vector2 worldOffset = new Vector2(transform.position.x * 0.01f, transform.position.z * 0.001f);
            rend.sharedMaterial.mainTextureOffset = offset + worldOffset;
        }
        else
        {
            rend.sharedMaterial.mainTextureOffset = offset;
        }
    }
}
