using UnityEngine;

[CreateAssetMenu(fileName = "ShootConfiguration", menuName = "ScriptableObjects/WeaponsConfig", order = 2)]
public class ShootConfigurationScriptableObject : ScriptableObject
{
    [Header("Gun Type")]
    public GunType gunType;

    [Header("Ballistics")]
    public float BulletSpeed = 50f;
    public float BulletLifetime = 5f;
    public float Range = 100f;
    public float Damage = 10f;
    public LayerMask HitMask;
    public Vector3 Spread = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Fire Rate")]
    public float FireRate = 600f;
    public float shootCooldown = 0.5f;

    [Header("Magazine")]
    public int MagazineSize = 30;

    [Header("References")]
    public GameObject bulletPrefab;
    public float shootForce = 10f;
}
