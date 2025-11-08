using UnityEngine;
using VaultSystems.Data;
using System.Collections;
using System;
 
public class BulletPooled : MonoBehaviour
{
    [Header("Audio Impact")]
    public AudioSource hitAudioSource;
    public AudioClip impactClip;
    [Range(0.1f, 1f)] public float minPitch = 0.8f;
    [Range(0.5f, 2f)] public float returnSpeed = 2f;

    private float speed;
    private float lifetime;
    private int damage;
    private float spawnTime;
    private Action<BulletPooled> onReleaseToPool;
    private Collider shooterCollider; //shooter's collider to ignore
    public void Initialize(Action<BulletPooled> releaseCallback, float bulletSpeed, float bulletLifetime, int bulletDamage, Collider shooterCollider = null)

    {
        onReleaseToPool = releaseCallback;
        speed = bulletSpeed;
        lifetime = bulletLifetime;
        damage = bulletDamage;
        spawnTime = Time.time;
        this.shooterCollider = shooterCollider;  // set shooter's collider to ignore 

        gameObject.SetActive(true);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        // lifetime check
        if (Time.time - spawnTime >= lifetime)
            Release();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore the shooter
        if (shooterCollider != null && other == shooterCollider) return;
    
        if (!other.TryGetComponent<IDamageable>(out var target)) return;
        if (target.IsDead) return;

        int previousHP = target.CurrentHP;
        target.TakeDamage(damage);

        Debug.Log($"[Bullet] Hit '{other.name}': {previousHP} → {target.CurrentHP} HP (took {damage})");

        if (hitAudioSource != null && impactClip != null)
            StartCoroutine(PlayImpactSoundWithPitchDip());

        Release(); // release instead of destroy
    }

    private IEnumerator PlayImpactSoundWithPitchDip()
    {
        float originalPitch = hitAudioSource.pitch;
        float deepPitch = UnityEngine.Random.Range(minPitch, originalPitch);
        hitAudioSource.pitch = deepPitch;
        hitAudioSource.PlayOneShot(impactClip);

        yield return new WaitForSeconds(impactClip.length / 2f);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            hitAudioSource.pitch = Mathf.Lerp(deepPitch, originalPitch, t);
            yield return null;
        }

        hitAudioSource.pitch = originalPitch;
    }

    public void ResetBullet()
    {
        StopAllCoroutines();
        if (hitAudioSource) hitAudioSource.Stop();
        transform.rotation = Quaternion.identity;
        spawnTime = 0f;
        speed = 0f;
        lifetime = 0f;
        damage = 0;
        shooterCollider = null; //clear reference to shooter collider
        onReleaseToPool = null;
    }

    private void Release()
    {
        ResetBullet();
        onReleaseToPool?.Invoke(this);
    }
}
