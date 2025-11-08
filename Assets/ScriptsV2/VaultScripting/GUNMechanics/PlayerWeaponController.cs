using UnityEngine;
using VaultSystems.Controllers;
using VaultSystems.Data;
using VaultSystems.Invoker;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VaultSystems.Data
{
    /// <summary>
    /// Manages weapon equipping, firing, and IK for the player.
    /// Integrates with PlayerCaseController, PlayerAnimator1, and WeaponIKController.
    /// </summary>
    [DefaultExecutionOrder(35)]
    [RequireComponent(typeof(PlayerCaseController))]
    public class PlayerWeaponController : MonoBehaviour
    {
        #region === REFERENCES ===

        [Header("Core References")]

        
        public PlayerController playerController;
        public PlayerCaseController caseController;
        public PlayerAnimator1 playerAnimator;
        public WeaponIKController weaponIK;

        [Header("Hand IK Targets")]
        public Transform rightHandTarget;
        public Transform leftHandTarget;

        #endregion

        #region === WEAPON STATE ===

        [Header("Current Weapon")]
        private WeaponData currentWeapon;
        private GameObject currentWeaponInstance;
        private bool isWeaponEquipped = false;
        private float lastFireTime = 0f;

        [Header("Animation Blending")]
        private float weaponLayerWeight = 0f;
        private const float layerBlendSpeed = 5f;
        private float targetLayerWeight = 0f;

        #endregion

        #region === AMMO ===

        [Header("Ammo Management")]
        private Dictionary<string, int> ammoInventory = new();
        private const int AMMO_RESERVE_MULTIPLIER = 3; // Ammo = max * 3

        #endregion

        #region === EVENTS ===

        public event Action<WeaponData> OnWeaponEquipped;
        public event Action OnWeaponUnequipped;
        public event Action<string, int> OnAmmoChanged; // weaponID, ammoCount

        #endregion

        #region === LIFECYCLE ===

        private void Awake()
        {
            gameObject.tag = "Player";
            //if (playerController == null)
                //playerController = GetComponentInChildren<PlayerController>();

              //  playerController = FindObjectsOfType<PlayerController>(true);
            
            InitializeReferences();
            ammoInventory.Clear();
        }

        private void Start()
        {
            RegisterWithWorldBridge();
        }

        private void Update()
        {
            if (!isWeaponEquipped) return;

            // Smooth weapon layer blending
            BlendWeaponLayer();

            // Handle firing input (only when in combat case)
            if (caseController.HasCase(PlayerCaseController.PlayerCase.Combat))
                HandleWeaponInput();
        }

        private void LateUpdate()
        {
            // Ensure IK targets stay synced if weapon moves (rare but safe)
            if (isWeaponEquipped && currentWeaponInstance != null && weaponIK != null)
                UpdateIKTargets();
        }

        #endregion

        #region === INITIALIZATION ===

        private void InitializeReferences()
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null)
                Debug.LogError("[PlayerWeaponController] PlayerController not found!");

            caseController = GetComponent<PlayerCaseController>();
            if (caseController == null)
                Debug.LogError("[PlayerWeaponController] PlayerCaseController not found!");

            playerAnimator = GetComponent<PlayerAnimator1>();
            if (playerAnimator == null)
                playerAnimator = GetComponentInChildren<PlayerAnimator1>();

            weaponIK = GetComponent<WeaponIKController>();
            if (weaponIK == null)
                weaponIK = GetComponentInChildren<WeaponIKController>();

            if (playerAnimator?.animator == null)
                Debug.LogWarning("[PlayerWeaponController] No Animator found!");

            if (weaponIK == null)
                Debug.LogWarning("[PlayerWeaponController] No WeaponIKController found!");
        }

        #endregion

        #region === EQUIP / UNEQUIP ===

        /// <summary>
        /// Equip a weapon. Called by inventory system or directly.
        /// </summary>
        public void EquipWeapon(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogWarning("[PlayerWeaponController] Cannot equip null weapon");
                return;
            }

            // Already equipped?
            if (isWeaponEquipped && currentWeapon == weaponData)
                return;

            // Unequip previous
            if (isWeaponEquipped)
                UnequipWeapon();

            currentWeapon = weaponData;
            isWeaponEquipped = true;

            // Instantiate weapon prefab
            if (weaponData.weaponPrefab != null)
            {
                currentWeaponInstance = Instantiate(
                    weaponData.weaponPrefab,
                    transform.position,
                    transform.rotation,
                    transform
                );
                currentWeaponInstance.name = weaponData.weaponID;
            }

            // Setup IK (hand positioning)
            SetupIKTargets();

            // Play equip animation
            PlayEquipAnimation();

            // Enter combat case
            caseController.PushCase(PlayerCaseController.PlayerCase.Combat);

            // Initialize ammo
            if (!ammoInventory.ContainsKey(weaponData.weaponID))
                ammoInventory[weaponData.weaponID] = weaponData.ammo * AMMO_RESERVE_MULTIPLIER;

            lastFireTime = Time.time;

            Debug.Log($"[PlayerWeaponController] Equipped '{weaponData.weaponID}' | Ammo: {ammoInventory[weaponData.weaponID]}");
            OnWeaponEquipped?.Invoke(weaponData);
        }

        /// <summary>
        /// Unequip current weapon.
        /// </summary>
        public void UnequipWeapon()
        {
            if (!isWeaponEquipped) 
                return;

            PlayUnequipAnimation();

            // Disable IK
            if (weaponIK != null)
                weaponIK.UnbindAllIK();

            // Exit combat case
            caseController.PopCase(PlayerCaseController.PlayerCase.Combat);

            // Cleanup
            if (currentWeaponInstance != null)
                Destroy(currentWeaponInstance);

            currentWeapon = null;
            currentWeaponInstance = null;
            isWeaponEquipped = false;
            targetLayerWeight = 0f;

            Debug.Log("[PlayerWeaponController] Weapon unequipped");
            OnWeaponUnequipped?.Invoke();
        }

        #endregion

        #region === IK SETUP ===

        private void SetupIKTargets()
        {
            if (weaponIK == null || currentWeapon == null || currentWeaponInstance == null)
                return;

            // Find hand targets on weapon prefab
            Transform rightTarget = currentWeaponInstance.transform.Find(currentWeapon.rightHandTargetName);
            Transform leftTarget = currentWeaponInstance.transform.Find(currentWeapon.leftHandTargetName);

            if (rightTarget == null || leftTarget == null)
            {
                Debug.LogWarning($"[PlayerWeaponController] Hand targets not found on weapon '{currentWeapon.weaponID}'");
                return;
            }

            rightHandTarget = rightTarget;
            leftHandTarget = leftTarget;

            // Bind to WeaponIKController
            weaponIK.BindFromWeapon(currentWeapon, currentWeaponInstance);
            weaponIK.EnableIK(true, 0.3f); // Smooth blend

            Debug.Log("[PlayerWeaponController] IK targets bound");
        }

        private void UpdateIKTargets()
        {
            // Re-sync targets in case weapon instance moves
            if (rightHandTarget != null && leftHandTarget != null && weaponIK != null)
                weaponIK.SetIKTargets(rightHandTarget, leftHandTarget);
        }

        #endregion

        #region === ANIMATION ===

        private void PlayEquipAnimation()
        {
            if (playerAnimator?.animator == null || currentWeapon == null)
                return;

            // Trigger equip
            playerAnimator.PlayTrigger("Equip");

            // Blend in weapon layer
            targetLayerWeight = 1f;
        }

        private void PlayUnequipAnimation()
        {
            if (playerAnimator?.animator == null)
                return;

            playerAnimator.PlayTrigger("Unequip");
            targetLayerWeight = 0f;
        }

        private void BlendWeaponLayer()
        {
            if (playerAnimator?.animator == null)
                return;

            weaponLayerWeight = Mathf.Lerp(
                weaponLayerWeight,
                targetLayerWeight,
                Time.deltaTime * layerBlendSpeed
            );

            // Set weapon layer weight (assuming layer 1 is weapon)
            playerAnimator.animator.SetLayerWeight(1, weaponLayerWeight);
        }

        private void PlayFireAnimation()
        {
            if (playerAnimator?.animator == null || currentWeapon == null)
                return;

            playerAnimator.PlayTrigger("Fire");
        }

        #endregion

        #region === FIRING ===

        private void HandleWeaponInput()
        {
            // Left mouse = fire
            if (Input.GetMouseButton(0))
                TryFire();
        }

        private void TryFire()
        {
            if (currentWeapon == null)
                return;

            // Check fire rate
            if (Time.time - lastFireTime < 1f / currentWeapon.fireRate)
                return;

            // Check ammo
            if (!HasAmmo(currentWeapon.weaponID))
            {
                PlayEmptyClick();
                return;
            }

            // Fire!
            Fire();
        }

        private void Fire()
        {
            if (currentWeapon == null)
                return;

            // Consume ammo
            ConsumeAmmo(currentWeapon.weaponID, 1);
            lastFireTime = Time.time;

            // Spawn bullet
            SpawnBullet();

            // Muzzle flash
            SpawnMuzzleFlash();

            // Sound
            PlayFireSFX();

            // Animation
            PlayFireAnimation();

            Debug.Log($"[PlayerWeaponController] FIRE! Ammo: {GetAmmoCount(currentWeapon.weaponID)}");
        }

        private void SpawnBullet()
        {
            if (currentWeapon == null || string.IsNullOrEmpty(currentWeapon.bulletPrefabName))
                return;

            Transform firePoint = GetFirePoint();
            if (firePoint == null)
                firePoint = transform;

            // Load bullet prefab
            GameObject bulletPrefab = Resources.Load<GameObject>($"Bullets/{currentWeapon.bulletPrefabName}");
            if (bulletPrefab == null)
            {
                Debug.LogError($"[PlayerWeaponController] Bullet prefab not found: Bullets/{currentWeapon.bulletPrefabName}");
                return;
            }

            // Spawn
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            // Set speed
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
                bullet.speed = currentWeapon.bulletSpeed;
        }

        private void SpawnMuzzleFlash()
        {
            if (currentWeapon == null || string.IsNullOrEmpty(currentWeapon.muzzleFlashPrefabName))
                return;

            Transform firePoint = GetFirePoint();
            if (firePoint == null)
                return;

            // Load muzzle flash prefab
            GameObject flashPrefab = Resources.Load<GameObject>($"Particles/{currentWeapon.muzzleFlashPrefabName}");
            if (flashPrefab == null)
            {
                Debug.LogWarning($"[PlayerWeaponController] Muzzle flash not found: Particles/{currentWeapon.muzzleFlashPrefabName}");
                return;
            }

            // Spawn and auto-destroy
            GameObject flash = Instantiate(flashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.5f); // Auto cleanup
        }

        private void PlayFireSFX()
        {
            if (currentWeapon == null || string.IsNullOrEmpty(currentWeapon.fireSFXName))
                return;

            // Load audio clip
            AudioClip clip = Resources.Load<AudioClip>($"SFX/{currentWeapon.fireSFXName}");
            if (clip == null)
            {
                Debug.LogWarning($"[PlayerWeaponController] Fire SFX not found: SFX/{currentWeapon.fireSFXName}");
                return;
            }

            Transform firePoint = GetFirePoint();
            if (firePoint == null)
                firePoint = transform;

            // Play spatial audio
            AudioSource.PlayClipAtPoint(clip, firePoint.position, currentWeapon.fireVolume);
        }

        private void PlayEmptyClick()
        {
            AudioClip clip = Resources.Load<AudioClip>("SFX/Empty_Click");
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, transform.position, 0.5f);
        }

        private Transform GetFirePoint()
        {
            if (currentWeaponInstance == null || currentWeapon == null)
                return null;

            return currentWeaponInstance.transform.Find(currentWeapon.firePointName);
        }

        #endregion

        #region === AMMO ===

        /// <summary>
        /// Add ammo for a specific weapon.
        /// </summary>
        public void AddAmmo(string weaponID, int amount)
        {
            if (!ammoInventory.ContainsKey(weaponID))
                ammoInventory[weaponID] = 0;

            ammoInventory[weaponID] += amount;
            OnAmmoChanged?.Invoke(weaponID, ammoInventory[weaponID]);
        }

        /// <summary>
        /// Consume ammo from current weapon.
        /// </summary>
        public void ConsumeAmmo(string weaponID, int amount)
        {
            if (ammoInventory.TryGetValue(weaponID, out int current))
            {
                ammoInventory[weaponID] = Mathf.Max(0, current - amount);
                OnAmmoChanged?.Invoke(weaponID, ammoInventory[weaponID]);
            }
        }

        /// <summary>
        /// Check if weapon has ammo.
        /// </summary>
        public bool HasAmmo(string weaponID)
        {
            return ammoInventory.TryGetValue(weaponID, out int count) && count > 0;
        }

        /// <summary>
        /// Get current ammo count.
        /// </summary>
        public int GetAmmoCount(string weaponID)
        {
            return ammoInventory.TryGetValue(weaponID, out int count) ? count : 0;
        }

        #endregion

        #region === WORLD BRIDGE ===

        private void RegisterWithWorldBridge()
        {
            var bridge = WorldBridgeSystem.Instance;
            if (bridge == null)
            {
                Debug.LogWarning("[PlayerWeaponController] WorldBridgeSystem not found");
                return;
            }

            // Register equip invoker for inventory system
            bridge.RegisterInvoker("player.weapon.equip", args =>
            {
                if (args.Length > 0 && args[0] is WeaponData weapon)
                    EquipWeapon(weapon);
            });

            // Register unequip invoker
            bridge.RegisterInvoker("player.weapon.unequip", _ => UnequipWeapon());

            // Register ammo add invoker
            bridge.RegisterInvoker("player.ammo.add", args =>
            {
                if (args.Length > 1 && args[0] is string weaponID && args[1] is int amount)
                    AddAmmo(weaponID, amount);
            });

            Debug.Log("[PlayerWeaponController] Registered with WorldBridgeSystem");
        }

        #endregion

        #region === DEBUG / CONTEXT MENU ===

        [ContextMenu("Debug/Equip Test Pistol")]
        private void DebugEquipPistol()
        {
            WeaponData testWeapon = Resources.Load<WeaponData>("Weapons/DefaultPistol");
            if (testWeapon != null)
                EquipWeapon(testWeapon);
            else
                Debug.LogError("[PlayerWeaponController] Test weapon not found in Resources/Weapons/DefaultPistol");
        }

        [ContextMenu("Debug/Unequip")]
        private void DebugUnequip()
        {
            UnequipWeapon();
        }

        [ContextMenu("Debug/Print Ammo")]
        private void DebugPrintAmmo()
        {
            Debug.Log("=== AMMO INVENTORY ===");
            foreach (var kv in ammoInventory)
                Debug.Log($"{kv.Key}: {kv.Value}");
        }

        [ContextMenu("Debug/Add Test Ammo")]
        private void DebugAddAmmo()
        {
            if (currentWeapon != null)
                AddAmmo(currentWeapon.weaponID, 30);
            else
                Debug.LogWarning("[PlayerWeaponController] No weapon equipped");
        }

        #endregion
    }
}