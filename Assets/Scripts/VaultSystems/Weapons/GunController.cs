using UnityEngine;
using System;
using VaultSystems.Invoker;
using System.Collections.Generic;
using System.Linq;
using VaultSystems.Data;
using VaultSystems.Containers;

public partial class GunController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool usePlayerCaseControl = true;
    [SerializeField] private float aimSwaySmoothing = 0.15f;
    [SerializeField] private float mouseSwaySensitivity = 0.01f;
    [SerializeField] private float mouseSwaySmoothing = 0.1f;
    [SerializeField] private float maxMouseSwayOffset = 0.1f;

    private GunScriptableObject currentGun;
    private float lastFireTime;
    private PlayerAnimator1 playerAnimator;
    private PlayerStatContext playerStatContext;
    private int ammoInMagazine;
    private int ammoInReserve;
    private bool isReloading;

    private GameObject gunModel;
    private bool isAiming;
    private float aimSwayTimer;
    private Vector2 mouseSwayOffset;
    private Vector2 targetMouseSwayOffset;

    private Dictionary<GunScriptableObject, AmmoState> ammoPerGun = new();

    public event Action<int> OnAmmoChanged;
    public event Action<int> OnReserveAmmoChanged;
    public event Action OnReloadStarted;
    public event Action OnReloadCompleted;
    public event Action<bool> OnAimStateChanged;

    private bool HasWeapon => currentGun != null;
    public int AmmoInReserve => ammoInReserve;
    public bool IsReloading => isReloading;
    public int AmmoInMagazine => ammoInMagazine;
    public GunScriptableObject CurrentGun => currentGun;
    public bool IsAiming => isAiming;
    public PlayerDataContainer playerData;
    public GameObject GetGunModel() => gunModel;
    public PlayerWeaponEventDataContainer weaponEventContainer;
    public struct AmmoState
    {
        public int ammoInMagazine;
        public int ammoInReserve;
    }

    private void OnEnable()
    {
        
    var weaponEventContainer = new PlayerWeaponEventDataContainer(playerData, this);
    weaponEventContainer.RegisterWithWorldBridge();


    
        RegisterWithCaseController();
        lastFireTime = -1f;
        playerAnimator = GetComponent<PlayerAnimator1>();
        playerStatContext = GetComponent<PlayerStatContext>();
    }

    private void Update()
    {
        if (HasWeapon && gunModel != null)
        {
            if (isAiming)
            {
                UpdateMouseSway();
                ApplyAimingSway();
            }
            else
            {
                ResetMouseSway();
                ResetGunPosition();
            }
        }
    }
private void RegisterWithCaseController()
{
    var caseCtrl = PlayerCaseController.Instance;
    if (caseCtrl == null) return;

    caseCtrl.RegisterCaseAction(PlayerCaseController.PlayerCase.Aim, () => SetAiming(true));
    
    caseCtrl.RegisterCaseAction(PlayerCaseController.PlayerCase.None, () => 
    {
        if (!caseCtrl.HasCase(PlayerCaseController.PlayerCase.Aim))
            SetAiming(false);
    });
}

    public void SetAiming(bool aiming)
    {
        if (aiming == isAiming) return;

        isAiming = aiming;
        aimSwayTimer = 0f;
        OnAimStateChanged?.Invoke(isAiming);

        // Update PlayerAnimator1's isAiming
        var playerAnimator = GetComponent<PlayerAnimator1>();
        if (playerAnimator != null)
        {
            playerAnimator.isAiming = aiming;
        }

        if (!isAiming)
            ResetGunPosition();
    }

    private void UpdateMouseSway()
    {
        if (playerData == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Scale sensitivity by level and wepSkill (higher values = less sway)
        float levelScaledSensitivity = mouseSwaySensitivity / Mathf.Sqrt(playerData.level + playerData.wepSkill + 1);

        targetMouseSwayOffset.x += mouseX * levelScaledSensitivity;
        targetMouseSwayOffset.y += mouseY * levelScaledSensitivity;

        // Clamp to max offset
        targetMouseSwayOffset.x = Mathf.Clamp(targetMouseSwayOffset.x, -maxMouseSwayOffset, maxMouseSwayOffset);
        targetMouseSwayOffset.y = Mathf.Clamp(targetMouseSwayOffset.y, -maxMouseSwayOffset, maxMouseSwayOffset);

        // Smooth towards target
        mouseSwayOffset = Vector2.Lerp(mouseSwayOffset, targetMouseSwayOffset, Time.deltaTime / mouseSwaySmoothing);
    }

    private void ResetMouseSway()
    {
        targetMouseSwayOffset = Vector2.zero;
        mouseSwayOffset = Vector2.Lerp(mouseSwayOffset, Vector2.zero, Time.deltaTime / mouseSwaySmoothing);
    }

    private void ApplyAimingSway()
    {
        if (playerStatContext == null) return;

        aimSwayTimer += Time.deltaTime;

        float swayAmplitude = playerStatContext.GetAimSwayAmplitude();
        float swayFrequency = playerStatContext.GetAimSwaySpeedMultiplier();

        float timeOffsetX = Mathf.Sin(aimSwayTimer * swayFrequency) * swayAmplitude;
        float timeOffsetY = Mathf.Cos(aimSwayTimer * swayFrequency * 0.7f) * swayAmplitude * 0.5f;
        float timeOffsetZ = Mathf.Sin(aimSwayTimer * swayFrequency * 0.5f) * swayAmplitude * 0.3f;

        // Combine time-based and mouse-based offsets
        float finalOffsetX = timeOffsetX + mouseSwayOffset.x;
        float finalOffsetY = timeOffsetY + mouseSwayOffset.y;
        float finalOffsetZ = timeOffsetZ;

        gunModel.transform.localPosition = Vector3.Lerp(
            gunModel.transform.localPosition,
            new Vector3(finalOffsetX, finalOffsetY, finalOffsetZ),
            Time.deltaTime / aimSwaySmoothing
        );
    }

    private void ResetGunPosition()
    {
        if (gunModel != null)
        {
            gunModel.transform.localPosition = Vector3.Lerp(
                gunModel.transform.localPosition,
                Vector3.zero,
                Time.deltaTime * 5f
            );
        }
    }

    public bool EquipGun(GunScriptableObject gun)
    {
        if (gun == null) return false;
        if (currentGun != null) UnequipGun();

        currentGun = gun;

        if (!ammoPerGun.ContainsKey(gun))
        {
            ammoPerGun[gun] = new AmmoState
            {
                ammoInMagazine = gun.ShootConfig.MagazineSize,
                ammoInReserve = gun.ShootConfig.MagazineSize * 3
            };
        }

        var state = ammoPerGun[gun];
        ammoInMagazine = state.ammoInMagazine;
        ammoInReserve = state.ammoInReserve;

        playerAnimator?.SetIKLayerActive(true);

        currentGun.InitializePools(transform);
        currentGun.InstantiateModel(transform);
        gunModel = currentGun.GetGunModel();
         // IMPORTANT: Keep gun as kinematic so it moves with player
    Rigidbody[] rbComps = gunModel.GetComponentsInChildren<Rigidbody>();
    foreach (var rb in rbComps)
    {
        rb.isKinematic = true;  // Lock it to player movement
        
    }
        isReloading = false;
        lastFireTime = -1f;
        isAiming = false;
        aimSwayTimer = 0f;
        mouseSwayOffset = Vector2.zero;
        targetMouseSwayOffset = Vector2.zero;

        OnAmmoChanged?.Invoke(ammoInMagazine);
        OnReserveAmmoChanged?.Invoke(ammoInReserve);
        Debug.Log($"[GunController] Equipped {gun.GunName}");
        return true;
    }

    public void UnequipGun()
    {
        if (currentGun != null)
        {
            ammoPerGun[currentGun] = new AmmoState
            {
                ammoInMagazine = ammoInMagazine,
                ammoInReserve = ammoInReserve
            };
            currentGun.CleanupModel();
        }

        playerAnimator?.SetIKLayerActive(false);
        currentGun = null;
        gunModel = null;
        isAiming = false;
        aimSwayTimer = 0f;
        mouseSwayOffset = Vector2.zero;
        targetMouseSwayOffset = Vector2.zero;
        OnAimStateChanged?.Invoke(false);
    }

    public Transform GetFirePoint()
    {
        if (currentGun == null)
        {
            Debug.LogWarning("[GunController] No weapon equipped.");
            return null;
        }
        return currentGun.GetFirePoint();
    }

    public bool TryFire(Vector3 firePosition, Vector3 fireDirection)
    {
        if (!HasWeapon)
        {
            Debug.LogWarning("[GunController] No weapon equipped.");
            return false;
        }

        if (usePlayerCaseControl && !CanFireByPlayerCase())
            return false;

        if (isReloading)
        {
            Debug.LogWarning("[GunController] Cannot fire while reloading.");
            return false;
        }

        if (ammoInMagazine <= 0)
        {
            Debug.LogWarning($"[GunController] No ammo in magazine.");
            return false;
        }

        float timeSinceLastFire = Time.time - lastFireTime;
        float fireRateDelay = 60f / currentGun.ShootConfig.FireRate;

        if (timeSinceLastFire < fireRateDelay)
            return false;

        Fire(firePosition, fireDirection);
        return true;
    }

    private bool CanFireByPlayerCase()
    {
        var caseCtrl = PlayerCaseController.Instance;
        if (caseCtrl == null) return true;

        var currentCase = caseCtrl.GetCurrentCase();
        return !PlayerCaseController.IsBlockingCase(currentCase);
    }

    private void Fire(Vector3 firePosition, Vector3 fireDirection)
    {
        var bulletGO = currentGun.GetBullet();
if (bulletGO == null) return;

var bullet = bulletGO.GetComponent<BulletPooled>();
if (bullet == null) return;

Collider shooterCollider = GetComponent<Collider>() ?? GetComponentInParent<Collider>();
bullet.Initialize(
    releasedBullet => currentGun.ReleaseBullet(releasedBullet),
    currentGun.ShootConfig.BulletSpeed,
    currentGun.ShootConfig.BulletLifetime,
    (int)currentGun.ShootConfig.Damage,
    shooterCollider
);

bullet.transform.position = firePosition;
bullet.transform.rotation = Quaternion.LookRotation(fireDirection.normalized);
bullet.gameObject.SetActive(true);


        bullet.transform.position = firePosition;
        bullet.transform.rotation = Quaternion.LookRotation(fireDirection.normalized);
        bullet.gameObject.SetActive(true);

        var trail = currentGun.GetTrail();
        if (trail != null)
        {
            trail.transform.SetParent(bullet.transform);
            trail.transform.localPosition = Vector3.zero;
        }

        playerAnimator?.PlayTrigger("Fire");
        PlayFireEffects();

        ammoInMagazine--;
        lastFireTime = Time.time;
        OnAmmoChanged?.Invoke(ammoInMagazine);

        Debug.Log($"[GunController] Fired {currentGun.GunName}. Ammo: {ammoInMagazine}/{currentGun.ShootConfig.MagazineSize}");
    }
public bool TryReload()
{
    if (!HasWeapon)
    {
        Debug.LogWarning("[GunController] No weapon equipped.");
        return false;
    }

    // ← ADD THIS CHECK (was missing!)
    if (usePlayerCaseControl && !CanReloadByPlayerCase())
        return false;

    if (isReloading)
    {
        Debug.LogWarning("[GunController] Already reloading.");
        return false;
    }

    if (ammoInMagazine == currentGun.ShootConfig.MagazineSize)
    {
        Debug.LogWarning("[GunController] Magazine already full.");
        return false;
    }

    // ← CHANGED: Use new modular method instead of direct check
    if (!HasReserveAmmo())
    {
        playerAnimator?.PlayTrigger("DryFire");
        Debug.LogWarning("[GunController] No reserve ammo.");
        return false;
    }

    playerAnimator?.PlayTrigger("Reload");
    StartCoroutine(ReloadCoroutine());
    return true;
}

private bool CanReloadByPlayerCase()
{
    var caseCtrl = PlayerCaseController.Instance;
    if (caseCtrl == null) return true;

    var currentCase = caseCtrl.GetCurrentCase();
    return !PlayerCaseController.IsBlockingCase(currentCase);
}

private System.Collections.IEnumerator ReloadCoroutine()
{
    isReloading = true;
    OnReloadStarted?.Invoke();

    if (currentGun.ReloadSound != null)
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.PlayOneShot(currentGun.ReloadSound);
            yield return new WaitForSeconds(currentGun.ReloadSound.length);
        }
        else
        {
            Debug.LogWarning("[GunController] AudioSource not found on player for reload sound.");
            yield return new WaitForSeconds(1f);
        }
    }
    else
    {
        Debug.LogWarning("[GunController] ReloadSound is null for " + currentGun.GunName);
        yield return new WaitForSeconds(1f);
    }

    if (currentGun.ShootConfig == null)
    {
        Debug.LogError("[GunController] ShootConfig is null for " + currentGun.GunName);
        isReloading = false;
        yield break;
    }

    int ammoNeeded = currentGun.ShootConfig.MagazineSize - ammoInMagazine;
    int ammoToRestore = Mathf.Min(ammoNeeded, ammoInReserve);

    ammoInMagazine += ammoToRestore;
    ammoInReserve -= ammoToRestore;

    isReloading = false;
    OnReloadCompleted?.Invoke();
    OnAmmoChanged?.Invoke(ammoInMagazine);
    OnReserveAmmoChanged?.Invoke(ammoInReserve);

    Debug.Log($"[GunController] {currentGun.GunName} reloaded. Magazine: {ammoInMagazine}, Reserve: {ammoInReserve}");
}

/// <summary>
/// Modular ammo system - can be upgraded to use inventory later
/// </summary>
private bool HasReserveAmmo() => ammoInReserve > 0;

public void AddReserveAmmo(int amount)
{
    if (!HasWeapon)
    {
        Debug.LogWarning("[GunController] Cannot add ammo - no weapon equipped.");
        return;
    }

    if (amount <= 0)
    {
        Debug.LogWarning("[GunController] Cannot add negative or zero ammo.");
        return;
    }

    ammoInReserve += amount;
    OnReserveAmmoChanged?.Invoke(ammoInReserve);
    Debug.Log($"[GunController] Added {amount} reserve ammo for {currentGun.GunName}. Total: {ammoInReserve}");
}

public void AddAmmo(int amount)
{
    if (!HasWeapon)
    {
        Debug.LogWarning("[GunController] Cannot add ammo - no weapon equipped.");
        return;
    }

    if (amount <= 0)
    {
        Debug.LogWarning("[GunController] Cannot add negative or zero ammo.");
        return;
    }

    // Try to fill magazine first
    int ammoToMagazine = Mathf.Min(amount, currentGun.ShootConfig.MagazineSize - ammoInMagazine);
    ammoInMagazine += ammoToMagazine;
    int remaining = amount - ammoToMagazine;

    // Rest goes to reserve
    ammoInReserve += remaining;

    OnAmmoChanged?.Invoke(ammoInMagazine);
    OnReserveAmmoChanged?.Invoke(ammoInReserve);
    Debug.Log($"[GunController] Added {amount} ammo. Magazine: {ammoInMagazine}, Reserve: {ammoInReserve}");
}

/// <summary>
/// Future-proofing: Can be overridden or modified for inventory system
/// </summary>
public int GetAvailableReserveAmmo() => ammoInReserve;
private void PlayFireEffects()
{
    if (currentGun.ShootSound != null)
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.PlayOneShot(currentGun.ShootSound);
        }
        else
        {
            Debug.LogWarning("[GunController] AudioSource not found on player for fire sound.");
        }
    }
    else
    {
        Debug.LogWarning("[GunController] ShootSound is null for " + currentGun.GunName);
    }

    if (currentGun.MuzzleFlashPrefab != null)
    {
        var muzzleFlash = Instantiate(currentGun.MuzzleFlashPrefab,
            transform.position + currentGun.MuzzleFlashPosition,
            Quaternion.identity);
        Destroy(muzzleFlash, 0.1f);
    }
    else
    {
        Debug.LogWarning("[GunController] MuzzleFlashPrefab is null for " + currentGun.GunName);
    }

    if (currentGun.ShellEjectPrefab != null)
    {
        var shell = Instantiate(currentGun.ShellEjectPrefab,
            transform.position + currentGun.ShellEjectPosition,
            Quaternion.identity);
        Destroy(shell, 2f);
    }
    else
    {
        Debug.LogWarning("[GunController] ShellEjectPrefab is null for " + currentGun.GunName);
    }
}

    
    private void OnDestroy()
    {
        UnequipGun(); weaponEventContainer.Cleanup();
    }
}

public partial class GunController : MonoBehaviour
{
    public enum GunState
    {
        Inactive,   // no weapon equipped
        Standby,    // equipped but idle
        Aiming,     // aiming down sights
        Firing,     // actively shooting
        Reloading,  // mid-reload
        Locked      // 🚫 input unavailable (e.g. in car, cinematic, UI)
    }

    public GunState CurrentState { get; private set; } = GunState.Inactive;
    private bool isFiring = false;

    // --- STATE MACHINE ---
    public void SetState(GunState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;

        switch (newState)
        {
            case GunState.Inactive:
                CleanupSystems();
                break;

            case GunState.Standby:
                DisableFiringSystems();
                break;

            case GunState.Aiming:
                EnableAimSystems();
                break;

            case GunState.Firing:
                EnableFiringSystems();
                break;

            case GunState.Reloading:
                DisableFiringSystems();
                break;

            case GunState.Locked:
                EnterLockedState();
                break;
        }

        Debug.Log($"[GunController] => State changed to {newState}");
    }

    private void DisableFiringSystems()
    {
        isFiring = false;
    }

    private void EnableAimSystems()
    {
        // optionally adjust FOV, camera, or aim offset
    }

    private void EnableFiringSystems()
    {
        isFiring = true;
        // optionally enable recoil controller or particle trail
    }

    private void EnterLockedState()
    {
        // Completely disable visuals, firing, and audio
        DisableFiringSystems();
        isFiring = false;

        // optional: hide crosshair or lower weapon model
        if (currentGun != null)
            currentGun.SetActive(false);
    }

    private void CleanupSystems()
    {
        DisableFiringSystems();
        if (currentGun != null)
            currentGun.SetActive(false);
    }

    // 🔓 Unlock from restricted state when player regains control
    public void UnlockFromCases()
    {
        if (CurrentState == GunState.Locked)
        {
            if (currentGun != null)
                currentGun.SetActive(true);

            SetState(GunState.Standby);
            Debug.Log("[GunController] Unlocked and returned to Standby");
        }
    }
}
