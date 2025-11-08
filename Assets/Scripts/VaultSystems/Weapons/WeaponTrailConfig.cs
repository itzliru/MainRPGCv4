using UnityEngine;

[CreateAssetMenu(fileName = "WeaponTrailConfig", menuName = "ScriptableObjects/WeaponTrailConfig", order = 4)]
public class WeaponTrailConfig : ScriptableObject
{
    public Material TrailMaterial;
    public float TrailWidth = 0.1f; 
    public AnimationCurve TrailWidthCurve = AnimationCurve.Linear(0, 1, 1, 0);

    public float MinVertexDistance = 0.1f;
    public float TrailLifetime = 0.5f;

    public float MissDistance = 100f;

    public float SimulationSpeed = 1f;
    public Color TrailColor = Color.white;
    public int TrailSegmentCount = 10;
}