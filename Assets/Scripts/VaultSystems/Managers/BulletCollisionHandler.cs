using UnityEngine;
using VaultSystems.Data;
[RequireComponent(typeof(Rigidbody), typeof(Collider))]


///currently not used or setup
public class BulletCollisionHandler : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float lifeTime = 5f;
    public float damage = 25f;
    public LayerMask hitMask; // Defaults to GlobalLayerMaskManager.Exclude(GlobalLayerMaskManager.Player)
    public GameObject impactEffect;

    private GameObject owner;
    private Rigidbody rb;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Default layer filtering — everything except projectiles & player
        if (hitMask == 0)
        {
            hitMask = GlobalLayerMaskManager.Exclude(
                GlobalLayerMaskManager.Player,
                GlobalLayerMaskManager.Projectile
            );
        }
    }

    public void Initialize(GameObject shooter, Vector3 direction, float speed)
    {
        owner = shooter;
        rb.velocity = direction.normalized * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return; // Prevent double-hit bug
        hasHit = true;

        GameObject hitObject = collision.gameObject;

        // Ignore hitting the owner
        if (hitObject == owner)
        {
            Physics.IgnoreCollision(hitObject.GetComponent<Collider>(), GetComponent<Collider>());
            return;
        }

        // Layer filtering
        if ((hitMask.value & (1 << hitObject.layer)) == 0)
        {
            Debug.Log($"[Bullet] Ignored collision with {hitObject.name} (Layer: {LayerMask.LayerToName(hitObject.layer)})");
            return;
        }

        // Apply damage


        // Spawn impact FX
        if (impactEffect)
            Instantiate(impactEffect, collision.contacts[0].point, Quaternion.identity);

        // Optional: stick or bounce behavior
        // rb.isKinematic = true; transform.parent = hitObject.transform;

        Destroy(gameObject, 0.02f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, rb != null ? rb.velocity.normalized * 1f : transform.forward);
    }
}
