using UnityEngine;
using VaultSystems.Data;
using System.Collections;

public class Bullet : MonoBehaviour
{
    [Header("Movement & Damage")]
    public float speed = 20f;
    public float lifetime = 5f;
    public int damage = 7;

    [Header("Audio Impact")]
    private AudioSource hitAudioSource;
    private AudioClip impactClip;
    [Range(0.1f, 1f)] public float minPitch = 0.8f;
    [Range(0.5f, 2f)] public float returnSpeed = 2f;

    private void Start()
    {
        Destroy(gameObject, lifetime); // auto-cleanup
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<IDamageable>(out var target)) return;
        if (target.IsDead) return;

        int previousHP = target.CurrentHP;
        target.TakeDamage(damage);

        // ✅ Log hit info
        Debug.Log($"[Bullet] Hit '{other.name}': {previousHP} → {target.CurrentHP} HP " +
                  $"(took {damage} damage)");

        // ✅ Play impact sound with pitch dip
        if (hitAudioSource != null && impactClip != null)
            StartCoroutine(PlayImpactSoundWithPitchDip());

        Destroy(gameObject);
    }

    private IEnumerator PlayImpactSoundWithPitchDip()
    {
        // Store original pitch
        float originalPitch = hitAudioSource.pitch;

        // Apply random deep pitch
        float deepPitch = Random.Range(minPitch, originalPitch);
        hitAudioSource.pitch = deepPitch;

        // Play the sound
        hitAudioSource.PlayOneShot(impactClip);

        // Wait while playing
        yield return new WaitForSeconds(impactClip.length / 2f);

        // Smoothly restore pitch
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            hitAudioSource.pitch = Mathf.Lerp(deepPitch, originalPitch, t);
            yield return null;
        }

        hitAudioSource.pitch = originalPitch;
    }
}
