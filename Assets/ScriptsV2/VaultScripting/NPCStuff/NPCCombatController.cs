
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using VaultSystems.Data;
using VaultSystems.Invoker;
public enum CombatMode
{
    Ranged,
    SearchAmmo,
    Melee
}
namespace VaultSystems.Data
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NPCEquipmentManager))]
    [RequireComponent(typeof(NPCAgentController))]
    public class NPCCombatController : MonoBehaviour

    {
        [Header("Audio")]
        public string fireSFXName = "Gunshot_Default";
        public string reloadSFXName = "Reload_Default";
        public string emptySFXName = "Empty_Click";
        private AudioSource sfxSource;
       
        [Header("Combat State")]
        public bool hasWeapon = false;
        public bool isAttacking = false;
        public Transform target;

        [Header("Vision Settings")]
        public float visionRange = 12f;
        public float visionAngle = 120f;
        public float visionLoseTime = 6f;
        public LayerMask targetMask;
        public LayerMask obstructionMask;

        [Header("Search Behavior")]
        public float searchDuration = 6f;
        public float searchRadius = 5f;
        public float searchPause = 1.2f;

        [Header("Weapon Behavior")]
        public float weaponIdleTimeout = 6f;
        public float attackRange = 2.5f;
        private float lastFireTime = 0f;
        [Header("Debug")]
        public bool drawVision = true;
        public Color visionColor = Color.yellow;

        private int noAmmoSearchAttempts = 0;
        private const int maxAmmoSearchAttempts = 2;
        private float baseAmmoSearchRadius = 15f;
        private float currentAmmoSearchRadius = 15f;

        public event System.Action SwitchToFistCombat;

        [Header("Runtime References")]
        public Animator animator; // 👈 Animator on npc
        public NPCEquipmentManager equipManager;  // 👈 NPCEquipmentManager.cs
        public NPCAgentController agentController; // 👈 NPCAgentController.cs
        public WeaponIKController ik; // 👈 WeaponIKController.cs
        public NavMeshAgent navAgent; // 👈 navAgent we have 2 scripts
        private GOAPAgent goapAgent; //👈 GOAPAgent.cs
        public NPCDataContainer npcData; // 👈 link to data
        public FistCombatController FistCombatController; // 👈 link to fistycuffs
        public HumanoidAimSolver aimSolver;// 👈 aim ik constraint and rig via humanbones[]

        public bool combatEnabled = false;
        public float idleTimer;
        public float lostSightTimer;
        public bool searching;
        public Vector3 lastKnownPosition;
        public Transform currentTarget;
        ////Movment stuff
        private Vector3 strafeDestination;
        private float strafeTimer;

        // === Public read-only accessors for GOAP & external systems ===
        public bool IsSearching => searching;
        public Vector3 LastKnownPosition => lastKnownPosition;

        public WeaponData equippedWeapon => equipManager?.currentWeaponData;
        public GameObject equippedWeaponInstance => equipManager?.CurrentWeaponInstance;

        private void Awake()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            animator = GetComponent<Animator>();
            equipManager = GetComponent<NPCEquipmentManager>();
            agentController = GetComponent<NPCAgentController>();
            ik = GetComponent<WeaponIKController>();
            navAgent = GetComponent<NavMeshAgent>();
            goapAgent = GetComponent<GOAPAgent>(); // for goap additive
            npcData = GetComponent<NPCDataContainer>();
            if (aimSolver == null)
                aimSolver = GetComponent<HumanoidAimSolver>();
            var bridge = VaultSystems.Invoker.WorldBridgeSystem.Instance;
            if (bridge != null)
            {
                bridge.RegisterID(npcData.npcId, npcData);
                Debug.Log($"[NPCCombatController] Registered NPC ID {npcData.npcId} with WorldBridgeSystem");
            }


        }

        private void OnEnable()
    {
        npcData = GetComponent<NPCDataContainer>();
        if (npcData != null)
        {
            npcData.OnDeathStateChanged += HandleDeathStateChange;
        }
    }

    private void OnDisable()
    {
        if (npcData != null)
        {
            npcData.OnDeathStateChanged -= HandleDeathStateChange;
        }
    }

    private void HandleDeathStateChange(DeathState newState)
    {
        switch (newState)
        {
            case DeathState.Alive:
                DisableCombatModule();
                break;
            case DeathState.Dying:
                // Optionally play death animation
                break;
            case DeathState.RagdollActive:
                DisableCombatModule();
                // Ragdoll is now active
                break;
            case DeathState.Dead:
                // Schedule despawn or cleanup
                break;
        }
    }


        // Enable aiming when using weapon


        public void EnableAimSolver()
        {
            if (aimSolver != null)
                aimSolver.solverEnabled = true;
        }

        // Disable aiming when switching to fists/melee
        public void DisableAimSolver()
        {
            if (aimSolver != null)
                aimSolver.solverEnabled = false;
        }

        private void Start()
        {
            // On start, sync with container state
            if (npcData != null)
                npcData.UpdateCombatStateFromData();

            SwitchToFistCombat += () =>
         {
             var fists = GetComponent<FistCombatController>();
             if (fists != null)
             {
                 fists.EnableFistMode();
                 fists.EngageFistTarget(currentTarget);
             }
         };


        


        }

        private void Update()
        {
            if (!combatEnabled)
                return;

            VisionCheck();
            UpdateWorldState();
            if (IsTargetVisible(currentTarget))
            {
                lostSightTimer = 0f;
                searching = false;
                lastKnownPosition = currentTarget.position;
                EngageTarget();
            }
            else
            {
                lostSightTimer += Time.deltaTime;

                if (lostSightTimer > visionLoseTime && !searching && lastKnownPosition != Vector3.zero)
                {
                    StartCoroutine(SearchLastKnown());
                }

                HandleIdleState();
            }
        }
        // ==============================
        // 🔄 ENABLE / DISABLE MODULE API
        // ==============================

        /// <summary>
        /// Public method to update combat AI. Called by external systems during case management.
        /// Performs vision checks and world state updates.
        /// </summary>
        public void UpdateAI()
        {
            if (!combatEnabled)
                return;

            VisionCheck();
            UpdateWorldState();
        }

        public void EnableCombatModule()
        {
            if (combatEnabled) return;

            combatEnabled = true;
            enabled = true;
            if (ik != null)
                ik.EnableHumanoidSolver(true);
            Debug.Log($"[NPCCombatController] Combat module ENABLED for {gameObject.name}");
            StopAllCoroutines();
            lostSightTimer = 0;
            searching = false;
        }

        public void DisableCombatModule()
        {
            if (!combatEnabled) return;

            combatEnabled = false;
            enabled = true; // Keep update alive for idle handling or disable entirely if desired
            if (ik != null)
                ik.EnableHumanoidSolver(false);

            StopAllCoroutines();
            agentController.Stop();
            UnequipWeapon();
            ik.aimEnabled = false;
            ik.aimTarget = null;
            currentTarget = null;

            Debug.Log($"[NPCCombatController] Combat module DISABLED for {gameObject.name}");
        }

        private bool IsTargetVisible(Transform t)
        {
            if (!t) return false;

            // ✅ NEW: Ignore dead targets
    if (IsTargetDead(t))
        return false;

            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 dir = (t.position - origin).normalized;
            float angle = Vector3.Angle(transform.forward, dir);
            float dist = Vector3.Distance(origin, t.position);

            // Vision cone limits
            if (dist > visionRange || angle > visionAngle * 0.5f)
            {
                // 🔴 Outside vision cone
                Debug.DrawLine(origin, t.position, Color.red, 0.1f);
                return false;
            }

            // Raycast obstruction check
            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstructionMask))
            {
                // 🔴 Hit something before the target
                Debug.DrawLine(origin, hit.point, Color.yellow, 0.1f);
                return false;
            }

            // 🟢 Clear line of sight
            Debug.DrawLine(origin, t.position, Color.green, 0.1f);
            return true;
        }

        /// <summary>
/// Check if a target is dead and should be ignored
/// </summary>
private bool IsTargetDead(Transform targetTransform)
{
    if (targetTransform == null)
        return true; // Null = treat as dead
    
    // Get the damageablecontainer from target
    BaseDamageableContainer damageable = targetTransform.GetComponent<BaseDamageableContainer>();
    
    if (damageable != null)
    {
        return damageable.IsDead; // ✅ Check if actually dead
    }
    
    // No damageable component = ignore it
    return true;
}

        private void TryFire()
        {
            if (equippedWeapon == null) return;
            if (npcData.currentAmmo <= 0) return;

            // Respect weapon fireRate
            if (Time.time - lastFireTime < 1f / equippedWeapon.fireRate)
                return;

            npcData.currentAmmo--;  // Consume ammo
            lastFireTime = Time.time;

            FireBullet();
            SpawnMuzzleFlash();
            PlayGunshotSFX(); // 🔊 <-- new
        }
private void PlayGunshotSFX()
{
    if (equippedWeapon == null || string.IsNullOrEmpty(equippedWeapon.fireSFXName))
        return;

    // Load AudioClip from Resources
    AudioClip clip = Resources.Load<AudioClip>($"SFX/{equippedWeapon.fireSFXName}");
    if (clip == null)
    {
        Debug.LogWarning($"[NPCShooter] Missing SFX at Resources/SFX/{equippedWeapon.fireSFXName}");
        return;
    }

    // Use a temporary AudioSource at the firePoint or self
    Transform firePoint = equippedWeaponInstance?.transform.Find(equippedWeapon.firePointName);
    if (firePoint == null)
        firePoint = transform;

    AudioSource.PlayClipAtPoint(clip, firePoint.position, 1.0f); // 1.0 = volume
}

        private void FireBullet()
        {
            if (equippedWeapon == null || string.IsNullOrEmpty(equippedWeapon.bulletPrefabName))
                return;

            // Find muzzle/firePoint
            Transform firePoint = equippedWeaponInstance?.transform.Find(equippedWeapon.firePointName);
            if (firePoint == null)
                firePoint = transform; // fallback to NPC's transform

            GameObject bulletObj = Instantiate(GetBulletPrefab(equippedWeapon.bulletPrefabName),
                                               firePoint.position, firePoint.rotation);

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
                bullet.speed = equippedWeapon.bulletSpeed;
        }
        // ---------------------
        // Muzzle flash spawn
        // ---------------------
        private void SpawnMuzzleFlash()
        {
            if (equippedWeapon == null || string.IsNullOrEmpty(equippedWeapon.muzzleFlashPrefabName))
                return;

            Transform firePoint = equippedWeaponInstance?.transform.Find(equippedWeapon.firePointName);
            if (firePoint == null)
                firePoint = transform;

            GameObject flashPrefab = Resources.Load<GameObject>($"Particles/{equippedWeapon.muzzleFlashPrefabName}");
            if (flashPrefab != null)
            {
                GameObject flash = Instantiate(flashPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.5f); // auto cleanup after 0.5s
            }
        }


        private GameObject GetBulletPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;

            // Try bullets first
            GameObject bulletPrefab = Resources.Load<GameObject>($"Bullets/{prefabName}");
            if (bulletPrefab != null)
                return bulletPrefab;

            // Fallback: particle prefab
            bulletPrefab = Resources.Load<GameObject>($"Particles/{prefabName}");
            if (bulletPrefab != null)
                return bulletPrefab;

            Debug.LogWarning($"Bullet or particle prefab '{prefabName}' not found in Resources/Bullets or Resources/Particles.");
            return null;
        }

        /////
        ///// GOAP AREA
        // Update GOAP world state safely
        private void UpdateWorldState()
        {
            if (goapAgent == null)
                return;

            goapAgent.worldState["HasTarget"] = currentTarget != null;
            goapAgent.worldState["Searching"] = searching;
            goapAgent.worldState["HasWeapon"] = hasWeapon;
            goapAgent.worldState["InRange"] = currentTarget && Vector3.Distance(transform.position, currentTarget.position) <= attackRange;
            goapAgent.worldState["IsAttacking"] = isAttacking;
        }


        // Weighted field-of-view vision system
        public void VisionCheck()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, visionRange, targetMask);

            Transform bestTarget = null;
            float bestScore = 0f;

            foreach (var hit in hits)
            {
                    //skip if dead target
                    if (IsTargetDead(hit.transform))
                    continue;

                Vector3 dir = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dir);
                float dist = Vector3.Distance(transform.position, hit.transform.position);

                // 🧭 Early reject: too far beyond range buffer
                if (dist > visionRange * 1.2f)
                    continue;

                // ✅ Within FOV
                if (angle < visionAngle * 0.5f)
                {
                    // ✅ Clear line of sight
                    if (!Physics.Raycast(transform.position + Vector3.up * 1.5f, dir, out RaycastHit block, visionRange, obstructionMask))
                    {
                        // Score based on distance + angle
                        float weight = Mathf.Clamp01(1f - (dist / visionRange)) * Mathf.Clamp01(1f - (angle / visionAngle));

                        if (weight > bestScore)
                        {
                            bestScore = weight;
                            bestTarget = hit.transform;
                        }
                    }
                }
            }

            // 🟢 If we have a valid visible target
            if (bestTarget != null)
            {
                float distToTarget = Vector3.Distance(transform.position, bestTarget.position);

                // ✅ Clamp pursuit distance (so they don't chase forever)
                if (distToTarget <= visionRange * 1.1f)
                {
                    currentTarget = bestTarget;
                    target = currentTarget;
                    lastKnownPosition = currentTarget.position;
                    lostSightTimer = 0f; // reset
                }
                else
                {
                    // 🧠 Target too far — start losing sight countdown
                    lostSightTimer += Time.deltaTime;
                    if (lostSightTimer >= visionLoseTime)
                    {
                        lastKnownPosition = currentTarget ? currentTarget.position : lastKnownPosition;
                        currentTarget = null;
                        target = null;
                    }
                }
            }
            else if (currentTarget != null)
            {
                // 🔴 No longer visible but was recently seen
                lostSightTimer += Time.deltaTime;

                if (lostSightTimer >= visionLoseTime)
                {
                    lastKnownPosition = currentTarget.position;
                    currentTarget = null;
                    target = null;
                    StartCoroutine(SearchLastKnown());
                }
            }
            else
            {
                // 🟡 No target at all
                lostSightTimer = 0f;
            }
        }


        public void EngageTarget()
        {
            if (!hasWeapon)
                EquipWeapon();

            CombatMode mode = DetermineCombatMode();

            switch (mode)
            {
                case CombatMode.Ranged:
                    HandleWeaponCombat(); EnableAimSolver();
                    break;

                case CombatMode.SearchAmmo:
                    GameObject ammoObj = HandleNoAmmoFallback(); DisableAimSolver();

                    if (ammoObj != null)
                    {
                        navAgent.updateRotation = true;
                        isAttacking = false;
                        Debug.Log($"[{name}] Heading to ammo: {ammoObj.name}");
                    }

                    // Start searching if target is missing or not visible
                    if (currentTarget == null || !IsTargetVisible(currentTarget) || lastKnownPosition == Vector3.zero)
                    {
                        if (!searching)
                            StartCoroutine(SearchLastKnown());
                    }
                    else
                    {
                        return;
                    }
                    break;

                case CombatMode.Melee:
                    if (npcData.currentAmmo <= 0)
                        UnequipWeapon(); DisableAimSolver();
                    SwitchToFistCombat?.Invoke();
                    break;
            }
        }

        public CombatMode DetermineCombatMode()
        {
            if (npcData.currentAmmo > 0 && equippedWeapon != null)
                return CombatMode.Ranged;


            if (FindClosestAmmoPickup(currentAmmoSearchRadius) != null)
                return CombatMode.SearchAmmo;

            if (npcData.currentAmmo <= 0)
                SwitchToFistCombat?.Invoke();
            return CombatMode.Melee;
        }

        // Inside NPCCombatController
        private GameObject FindClosestAmmoPickup(float radius = 15f)
        {
            var pickups = GameObject.FindGameObjectsWithTag("AmmoPickup");
            if (pickups.Length == 0) return null;

            GameObject closest = null;
            float bestDist = float.MaxValue;

            foreach (var pick in pickups)
            {
                float dist = Vector3.Distance(transform.position, pick.transform.position);

                // Only consider ammo within the search radius
                if (dist < radius && dist < bestDist)
                {
                    bestDist = dist;
                    closest = pick;
                }
            }

            return closest;
        }

        public GameObject HandleNoAmmoFallback()
        {
            noAmmoSearchAttempts++;
            currentAmmoSearchRadius = baseAmmoSearchRadius + (noAmmoSearchAttempts * 10f);

            GameObject ammoObj = FindClosestAmmoPickup(currentAmmoSearchRadius);

            if (ammoObj != null)
            {
                Debug.Log($"[{name}] Attempt {noAmmoSearchAttempts}: Found ammo within {currentAmmoSearchRadius}m.");
                agentController.SetDestination(ammoObj.transform.position);
                isAttacking = false;
                return ammoObj; // ✅ return object found
            }

            // --- Fallback to melee after failed attempts ---
            if (noAmmoSearchAttempts >= maxAmmoSearchAttempts)
            {
                Debug.Log($"[{name}] No ammo found after {noAmmoSearchAttempts} attempts — switching to melee combat.");

                UnequipWeapon();
                EngageTarget();
                if (npcData.currentAmmo <= 0)
                    UnequipWeapon(); DisableAimSolver();
                SwitchToFistCombat?.Invoke();

                noAmmoSearchAttempts = 0;
                currentAmmoSearchRadius = baseAmmoSearchRadius;
                return null;

            }

            return null; // ✅ covers all paths
        }

        public void HandleWeaponCombat()
        {
            // From here on, we have ammo and can continue attacking/moving/etc.
            if (currentTarget == null || !IsTargetVisible(currentTarget))
            {
                if (!searching && lastKnownPosition != Vector3.zero)
                    StartCoroutine(SearchLastKnown());

                return;
            }

            if (!IsTargetVisible(currentTarget))
            {
                currentTarget = null;
                target = null;
                navAgent.updateRotation = true;
                StartCoroutine(SearchLastKnown());
                return;

            }
            // ✅ Only continue if we actually have ammo
            if (npcData.currentAmmo > 0 && equippedWeapon != null)
            {
                if (goapAgent != null)
                    goapAgent.worldState["InRange"] = Vector3.Distance(transform.position, currentTarget.position) <= attackRange;

                Vector3 dir = currentTarget.position - transform.position;
                dir.y = 0;

                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3f);
                }
                //  if (npcData.currentHP <= npcData.maxHP * 0.35f)
                //{
                //  Vector3 safePosition = FindSafeSpot(); // you'll write this
                //  agentController.SetDestination(safePosition);
                //   return; // skip attacking
                //}
                ///


                ///////TARGET MOVEMENT LOGIC
                float dist = Vector3.Distance(transform.position, currentTarget.position);
                Vector3 toTarget = (currentTarget.position - transform.position).normalized;

                // --- Distance thresholds ---
                // --- Distance thresholds ---
                float tooClose = attackRange * 0.6f;   // inside danger zone
                float idealRange = attackRange * 0.9f; // where we want to strafe
                float tooFar = attackRange * 1.1f;     // need to move in


                // --- Movement Logic ---
                if (dist < tooClose)
                {
                    // 🟡 Too close — move backwards while facing target
                    Vector3 retreatDir = -toTarget; // backwards
                    Vector3 retreatPos = transform.position + retreatDir * 2f;

                    if (NavMesh.SamplePosition(retreatPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    {
                        // Move backwards but keep facing target
                        navAgent.updateRotation = true;
                        agentController.SetDestination(hit.position);
                        navAgent.updateRotation = false; // prevent agent from turning around
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), Time.deltaTime * 8f);
                    }
                }
                else if (dist > tooFar)
                {
                    // 🔵 Too far — move closer
                    agentController.SetDestination(currentTarget.position);
                    navAgent.updateRotation = true;
                }
                else
                {
                    if (strafeTimer <= 0f)
                    {
                        Vector3 strafeDir = Quaternion.Euler(0, Random.Range(-60f, 60f), 0) * toTarget;
                        Vector3 strafePos = transform.position + strafeDir * 2f;
                        if (NavMesh.SamplePosition(strafePos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                            strafeDestination = hit.position;

                        strafeTimer = Random.Range(1.5f, 3f); // new strafe every 1–3s
                    }

                    strafeTimer -= Time.deltaTime;
                    agentController.SetDestination(strafeDestination);
                }

                // ✅in ideal firing range
                if (dist <= idealRange && dist >= tooClose)
                {
                    if (npcData.currentAmmo <= 0)
                    {

                        EngageTarget(); return;
                    }
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), Time.deltaTime * 8f);
                }



                // 🎯 Aim IK
                // 🎯 Aim IK - AIM AT BODY CENTER (CHEST/HEAD), NOT ROOT
if (ik && currentTarget)
{
    // ✅ Target the enemy's center mass (chest area) instead of root
    Vector3 aimPoint = currentTarget.position + Vector3.up * 1.0f; // +1m for chest height
    
    // ✅ Create a separate aim target if it doesn't exist
    if (!ik.aimTarget)
    {
        GameObject aimTargetObj = new GameObject($"{currentTarget.name}_AimTarget");
        aimTargetObj.transform.position = aimPoint;
        ik.aimTarget = aimTargetObj.transform;
    }
    
    // ✅ Smoothly lerp aim point to target chest
    ik.aimTarget.position = Vector3.Lerp(ik.aimTarget.position, aimPoint, Time.deltaTime * 8f);
    
    Debug.DrawLine(ik.spineBone.position, aimPoint, Color.cyan);
    
    ik.aimEnabled = true;
    ik.BindAimRigConstraints();
}

                // 🔫 Attack only if standing in ideal range
                if (!isAttacking && dist <= idealRange && dist >= tooClose)
                {
                    StartCoroutine(AttackRoutine());
                }
                if (npcData.currentAmmo <= 0)
                    EngageTarget();
            }
        }

        public void HandleIdleState()
        {
            if (hasWeapon)
            {
                idleTimer += Time.deltaTime;
                if (idleTimer > weaponIdleTimeout)
                    UnequipWeapon();

            }

            if (ik)
            {
                ik.aimTarget = null;
                ik.aimEnabled = false;
            }
        }

        public IEnumerator SearchLastKnown()
        {
            searching = true;
            agentController.SetDestination(lastKnownPosition);

            float timer = 0f;
            while (timer < searchDuration)
            {
                if (currentTarget)
                {
                    searching = false;
                    yield break;
                }

                if (agentController.ReachedDestination())
                {
                    Vector3 random = lastKnownPosition + Random.insideUnitSphere * searchRadius;
                    if (NavMesh.SamplePosition(random, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                        agentController.SetDestination(hit.position);
                    navAgent.updateRotation = true;
                    yield return new WaitForSeconds(searchPause);
                }

                timer += Time.deltaTime;
                yield return null;
            }

            searching = false;
        }

        private WeaponData lastEquippedWeapon;

        public void EquipWeapon()
        {
            // Equip a random weapon
            equipManager.EquipRandomWeapon();

            WeaponData currentWeapon = equipManager.currentWeaponData;

            hasWeapon = true;
            animator.SetBool("HasWeapon", true);

            // Only set ammo if new weapon
            if (lastEquippedWeapon != currentWeapon && npcData != null)
                npcData.currentAmmo = currentWeapon.ammo;

            lastEquippedWeapon = currentWeapon;
        }


        private void UnequipWeapon()
        {
            hasWeapon = false;
            animator.SetBool("HasWeapon", false);
            equipManager.UnequipWeapon();
        }

        public IEnumerator AttackRoutine()
        {
            isAttacking = true;

            while (currentTarget != null && IsTargetVisible(currentTarget) && npcData.currentAmmo > 0)
            {
                animator.SetTrigger("Attack");
                TryFire();

                // Convert RPM to delay
                float fireDelay = 60f / equippedWeapon.fireRate; // 60 seconds / RPM
                yield return new WaitForSeconds(fireDelay);
            }

            isAttacking = false;
        }



        private void OnDrawGizmosSelected()
        {
            if (!drawVision) return;
            if (currentTarget)
                Gizmos.color = Color.red;
            else if (searching)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(transform.position, visionRange);

            Gizmos.color = visionColor;
            Gizmos.DrawWireSphere(transform.position, visionRange);

            Vector3 leftLimit = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward;
            Vector3 rightLimit = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + leftLimit * visionRange);
            Gizmos.DrawLine(transform.position, transform.position + rightLimit * visionRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.3f);
        }
    }
}