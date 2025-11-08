using VaultSystems.Controllers;
using UnityEngine;
using System;
using System.Collections;
using VaultSystems.Invoker;
using System.Threading.Tasks;
using VaultSystems.Weapons;
public class PlayerAnimator1 : MonoBehaviour
{
    [Header("Animation Parameters")]
    public string speedParam = "Speed";
    public string horizontalParam = "MoveX";
    public string verticalParam = "MoveZ";
    public bool use2DBlend = true;
    //trigger for aiming
    public bool isAiming;

    [Header("Backpack Handling")]
    [SerializeField] private GameObject backpackModel;

    [Header("Weapon IK")]
    [SerializeField] private PlayerWeaponIKController weaponIKController;

    [Header("Settings")]
    public float blendSmooth = 0.1f;      // Smoothing factor
    public float inputThreshold = 0.15f;  // Deadzone for small input noise
    public float rotationSpeed = 180f;  //degrees per sec
    public Animator animator;
    private Vector2 smoothedDir;
    private float smoothedSpeed;

    [Header("IK Layering")]
    public float ikLayerWeight = 0f;
    public float ikBlendSpeed = 5f;
    private float targetIKWeight = 0f;

    [Header("Backpack IK")]
    private Transform backpackIKTarget;
    private bool backpackIKActive = false;
    private float backpackIKWeight = 0f;
    private float backpackIKBlendSpeed = 5f;


    [Header("Physics")]
    public PlayerCaseController CaseLambdas;
    public PlayerController pc1;

    private void Awake()
    {
        // Grab animator from VRM or child
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogWarning("[PlayerAnimator] No Animator found on player or children!");

        CaseLambdas = GetComponent<PlayerCaseController>();
        if (CaseLambdas == null)
            Debug.LogWarning("[PlayerAnimator] No CaseLambdas found on player or children! ");

        pc1 = GetComponent<PlayerController>();
        if (pc1 == null)
            Debug.LogWarning("[PlayerAnimator] No pc1 found on player or children! ");

        // --- Layer Setup ---
        gameObject.layer = LayerMask.NameToLayer("Player");
        backpackModel = transform.Find("Inventory_Sack")?.gameObject;
        if (backpackModel != null)
        {
            backpackModel.SetActive(false);
            Debug.Log("[PlayerAnimator] Equipped backpack disabled at spawn");
        }

        // Create GunSocket if it doesn't exist
        Transform gunSocket = transform.Find("GunSocket");
        if (gunSocket == null)
        {
            gunSocket = new GameObject("GunSocket").transform;
            gunSocket.SetParent(transform);
            gunSocket.localPosition = Vector3.zero;
            gunSocket.localRotation = Quaternion.identity;
            Debug.Log("[PlayerAnimator] Created GunSocket");
        }

        // Parent GunSocket to player transform for IK compatibility
        if (gunSocket != null)
        {
            gunSocket.SetParent(transform);
            gunSocket.localPosition = Vector3.zero;
            gunSocket.localRotation = Quaternion.identity;
            Debug.Log("[PlayerAnimator] GunSocket parented to player transform");
        }
        else
        {
            Debug.LogWarning("[PlayerAnimator] GunSocket not found as child of player");
        }

        // Initialize weapon IK controller
        if (weaponIKController == null)
        {
            weaponIKController = GetComponent<PlayerWeaponIKController>();
        }
        if (weaponIKController != null)
        {
            weaponIKController.Initialize(this);
        }
    }



       
    private GameObject GetGunModel()
    {
        var gunController = GetComponent<GunController>();
        if (gunController == null) return null;
        return gunController.GetGunModel();
    }

    /// <summary>Get a socket by name from this transform</summary>
    private Transform GetSocket(string socketName)
    {
        return transform.Find(socketName);
    }

    public void SetIKLayerActive(bool active)
    {
        targetIKWeight = active ? 1f : 0f;
    }

    private void UpdateIKLayerBlending()
    {
        if (animator == null || animator.layerCount < 2) return;

        ikLayerWeight = Mathf.Lerp(ikLayerWeight, targetIKWeight, Time.deltaTime * ikBlendSpeed);
        animator.SetLayerWeight(1, ikLayerWeight);
    }

    private void UpdateBackpackIKBlending()
    {
        if (!backpackIKActive || animator == null) return;

        // Don't override weapon IK - backpack IK only when no weapon equipped
        if (ikLayerWeight > 0.01f) return;

        // Blend IK weight towards target
        backpackIKWeight = Mathf.Lerp(backpackIKWeight, 1f, Time.deltaTime * backpackIKBlendSpeed);

        // Apply IK to hands for backpack pickup (only when weapon IK not active)
        if (backpackIKTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, backpackIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, backpackIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, backpackIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, backpackIKTarget.rotation);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, backpackIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, backpackIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, backpackIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, backpackIKTarget.rotation);
        }
    }

    private void Update()
    {
        HandleInputAnimation();
        UpdateIKLayerBlending();
        //UpdateBackpackIKBlending();
        GetGunModel();

        // Set isAiming parameter for animator
        if (animator != null)
        {
            animator.SetBool("isAiming", isAiming);
        }
    }

    public void ResetAnimatorState()
    {
        if (animator == null) return;

        smoothedSpeed = 0f;
        smoothedDir = Vector2.zero;

        animator.SetFloat(speedParam, 0f);
        animator.SetFloat(horizontalParam, 0f);
        animator.SetFloat(verticalParam, 0f);

        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(param.name);
            else if (param.type == AnimatorControllerParameterType.Bool)
                animator.SetBool(param.name, false);
        }

        Debug.Log("[PlayerAnimator] Animator state reset");
    }

    public void HandleInputAnimation()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        float targetSpeed = new Vector2(moveX, moveZ).magnitude;

        if (Mathf.Abs(moveZ) < inputThreshold)
            moveZ = 0f;
        if (Mathf.Abs(moveX) < inputThreshold)
            moveX = 0f;

        Vector2 inputDir = new Vector2(moveX, moveZ);
        if (targetSpeed < inputThreshold)
            targetSpeed = 0f;

        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, Time.deltaTime / blendSmooth);
        smoothedDir.x = Mathf.Lerp(smoothedDir.x, moveX, Time.deltaTime / blendSmooth);
        smoothedDir.y = Mathf.Lerp(smoothedDir.y, moveZ, Time.deltaTime / blendSmooth);

        if (animator != null)
        {
            animator.SetFloat(speedParam, smoothedSpeed);

            if (use2DBlend)
            {
                animator.SetFloat(horizontalParam, smoothedDir.x);
                animator.SetFloat(verticalParam, smoothedDir.y);
            }
        }
    }

    /// <summary>
    /// Called by Initializer when player prefab is spawned.
    /// Ensures the animator is linked and ready.
    /// </summary>
    public void InitializeAnimator(Animator assignedAnimator = null)
    {
        if (assignedAnimator != null)
            animator = assignedAnimator;
        else if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            // Load from Resources if missing
            if (animator.runtimeAnimatorController == null)
            {
                var loadedController = Resources.Load<RuntimeAnimatorController>("AnimController/PlayerAnimator");
                if (loadedController != null)
                {
                    animator.runtimeAnimatorController = loadedController;
                    Debug.Log("[PlayerAnimator] Loaded Animator Controller from Resources.");
                }
                else
                {
                    Debug.LogWarning("[PlayerAnimator] Could not find Animator Controller in Resources/AnimController/PlayerAnimator");
                }
            }

            animator.applyRootMotion = false;
        }
        else
        {
            Debug.LogWarning("[PlayerAnimator] Animator not found!");
        }
    }

    /// <summary>
    /// External trigger support (for attacks, jumps, etc.)
    /// </summary>
    public void PlayTrigger(string triggerName)
    {
        if (animator != null)
            animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Initialize backpack IK for pickup animation
    /// </summary>
    public void InitializeBackpackIK(Transform targetSocket)
    {
        backpackIKTarget = targetSocket;
        backpackIKWeight = 0f;
    }

    /// <summary>
    /// Set backpack IK active/inactive
    /// </summary>
    public void SetBackpackIKActive(bool active)
    {
        backpackIKActive = active;
        if (!active)
        {
            backpackIKWeight = 0f;
            backpackIKTarget = null;
        }
    }

    /// <summary>
    /// Update backpack IK towards target socket (called by BackpackPickupIK)
    /// </summary>
    public void UpdateBackpackIKToSocket()
    {
        if (!backpackIKActive || backpackIKTarget == null || animator == null) return;

        // Blend IK weight towards target
        backpackIKWeight = Mathf.Lerp(backpackIKWeight, 1f, Time.deltaTime * backpackIKBlendSpeed);

        // Apply IK to hands for backpack pickup
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, backpackIKWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, backpackIKWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, backpackIKTarget.position);
        animator.SetIKRotation(AvatarIKGoal.RightHand, backpackIKTarget.rotation);

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, backpackIKWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, backpackIKWeight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, backpackIKTarget.position);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, backpackIKTarget.rotation);
    }
    public void SetBackpackVisible(bool visible)
    {
        // Find references once
        if (backpackModel == null)
            backpackModel = transform.Find("Inventory_Sack")?.gameObject;

        var packRoot = transform.Find("Lira_Player")?.gameObject;

        if (backpackModel != null && packRoot != null)
        {
            // Parent to player
            backpackModel.transform.SetParent(packRoot.transform);

            // Align perfectly with socket
            backpackModel.transform.localPosition = Vector3.zero;
            backpackModel.transform.localRotation = Quaternion.identity;

            // Toggle visibility
            backpackModel.SetActive(visible);

            Debug.Log($"[Backpack] {(visible ? "Shown" : "Hidden")} and attached to player socket!");
        }
        else
        {
            Debug.LogWarning("[Backpack] Missing model or playerRoot reference.");
        }
    }
}
