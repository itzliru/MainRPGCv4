using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Controllers;

namespace VaultSystems.Controllers
{
    [DefaultExecutionOrder(25)]
    
    public class PlayerController : MonoBehaviour 
    {
        private enum CamMode { FirstPerson, ThirdPerson, ThirdPersonAim, Death, Out }

        [Header("References")]
        public PlayerAnimator1 playerAnim;
        public PlayerCaseController caseController;
        public Rigidbody rb;
        public CapsuleCollider cc;
        [SerializeField] public GameObject rootObj;
        public Transform playerRoot;
        public CharacterController characterController;
        private float orbitYaw;
        /// <summary>
        /// States
        /// </summary>
        private bool IsPlayerDead => activeData != null && activeData.CurrentDeathState != DeathState.Alive;
        private PlayerStatContext statProvider;  // Cache stat calculations
        private PlayerDataContainer activeData;    // Direct reference for stats

        private SocketManager socketManager;    // Direct reference for stats

        /// <summary>
        /// Camera control variables
        /// </summary>

        private float orbitPitch;
        [Header("Camera Settings")]
        public Vector3 tpOffset = new Vector3(0, 1.3f, -1.575f);
        public Vector3 aimOffset = new Vector3(0.2f, 1.4f, -1.2f);
        public float camLerpSpeed = 7f;
        public bool startInThirdPerson = true;

        [Header("Dialogue & Interaction")]
        [SerializeField] private float dialogueFOV = 35f;  
        [SerializeField] private float dialogueFOVLerpSpeed = 5f;
        private float defaultFOV = 60f;  // Standard FPS camera FOV

        [Header("Mouse Look Settings")]
        public float mouseSensitivity = 100f;
        public float pitchClampMin = -90f;
        public float pitchClampMax = 90f;
        private float currentPitch = 0f;

        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float jumpForce = 5f;
        public float aimMoveSpeed = 4f;
        public LayerMask groundMask;
        private bool isGrounded;
        private Animator playerAnimator;
        [Header("Rotation & Smoothing")]
        public float velocitySmoothing = 0.1f;
        
        private CamMode currentCamMode;
        private Camera firstPersonCamera;
        private Camera thirdPersonCamera;
        private Camera deathCamera;
    private Camera outCamera;

        public Camera activeCamera;

        // Input buffer for FixedUpdate
        private Vector3 moveInput;
        private bool jumpInput;

        [Header("Death Camera")]
        public GameObject deathCameraGO;
        private bool hasHandledDeath = false;

        
        private void Awake()
        {
            caseController = GetComponent<PlayerCaseController>()
        ?? FindObjectOfType<PlayerCaseController>();  
        // ← Find singleton instead
    
        if (caseController == null)
             {
            Debug.LogError("[PlayerController] PlayerCaseController not found in scene!");
            }
            rb = GetComponentInChildren<Rigidbody>();
            cc = GetComponentInChildren<CapsuleCollider>();
            // playerAnim = GetComponentInChildren<Animator>();
            //characterController = GetComponentInChildren<CharacterController>();
          
             gameObject.tag = "Player";
            activeData = FindObjectsOfType<PlayerDataContainer>(true)
                 .FirstOrDefault(data => data.isActivePlayer);

              
                
            
              statProvider = GetComponent<PlayerStatContext>() 
        ?? GetComponentInChildren<PlayerStatContext>();
         if (statProvider == null)
            {
        Debug.LogWarning("[PlayerController] No IPlayerStatProvider found! Aiming stats won't be applied.");
            }

            if (groundMask == 0) groundMask = LayerMask.GetMask("Default", "Ground");
            

            // RB setup — physics safe, no interference
            cc.enabled = false;
            cc.isTrigger = true;
            rb.velocity = Vector3.zero;
            rb.useGravity = false;
            rb.drag = 5f;
            cc.enabled = true;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            currentCamMode = startInThirdPerson ? CamMode.ThirdPerson : CamMode.FirstPerson;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ResetPlayerState()
        {
            if (IsPlayerDead) return;
            hasHandledDeath = false;

            moveInput = Vector3.zero;
            jumpInput = false;
            isAimHeld = false;
            currentPitch = 0f;
            orbitPitch = 0f;
            orbitYaw = 0f;
            
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            if (characterController != null && characterController.enabled)
            {
                characterController.enabled = false;
                characterController.enabled = true;
            }
            
            if (caseController != null)
            {
                caseController.PopCase(PlayerCaseController.PlayerCase.Aim);
                caseController.PopCase(PlayerCaseController.PlayerCase.Combat);
            }
            
            currentCamMode = startInThirdPerson ? CamMode.ThirdPerson : CamMode.FirstPerson;
            SwitchActiveCamera();
            
            Debug.Log("[PlayerController] Player state reset");
        }


        private IEnumerator Start()
        {
            // Resolve playerRoot (_Player / outfit1)
            playerRoot = transform;
            Transform playerChild = transform.Find("_Player");
            if (playerChild != null)
            {
                playerRoot = playerChild;
                characterController = playerChild.GetComponent<CharacterController>();
                if (characterController == null)
                {
                    characterController = playerChild.gameObject.AddComponent<CharacterController>();
                    characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);

                    //characterController.center = new Vector3(0, 1.47f, 0);
                }
            }
            else
            {
                playerRoot = transform;
                characterController = GetComponent<CharacterController>()
                ?? gameObject.AddComponent<CharacterController>();
            }
            yield return new WaitForFixedUpdate();
            InitializeCameras();
            RegisterWorldBridge();
            playerAnimator = GetComponent<Animator>();
            // Setup Animator
            //if (playerAnim == null) playerAnim = GetComponent<Animator>();
            if (playerAnim?.animator != null) playerAnim.InitializeAnimator(playerAnim.animator);

            Debug.Log($"[PlayerController] Initialized on '{gameObject.name}'. Root: {playerRoot.name}, Cameras: FP={firstPersonCamera != null}, TP={thirdPersonCamera != null}");
        } 
        private float GetAimSwayAmount()
{
    if (statProvider == null) 
        return 0.5f;  // Fallback default

            return statProvider.GetAimSwayAmplitude();
}

private float GetAimSwaySpeed()
{
    if (statProvider == null) 
        return 2f;  // Fallback default
    
    return statProvider.GetAimSwaySpeedMultiplier();
}

private float GetAimMovementSpeedModifier()
{
    if (statProvider == null) 
        return 4f;  // Fallback default
    
    return statProvider.GetAimMovementSpeed();
}
 
        private void InitializeCameras()
        {

            // Baked FP_Socket — keep parented (local space)
            Transform fpSocket = playerRoot.Find("FP_Socket");
            if (fpSocket != null)
            {
                firstPersonCamera = fpSocket.GetComponent<Camera>();
                if (firstPersonCamera == null)
                    Debug.LogWarning("[PlayerController] FP_Socket missing Camera component.");
            }
            else
            {
                // Fallback creation
                Debug.LogWarning("[PlayerController] FP_Socket not found — creating fallback.");
                GameObject fpGo = new GameObject("FP_Camera");
                fpGo.transform.SetParent(playerRoot);
                fpGo.transform.localPosition = new Vector3(0, 1.6f, 0.1f);
                fpGo.transform.localRotation = Quaternion.identity;
                firstPersonCamera = fpGo.AddComponent<Camera>();
            }

            // Baked TP_Socket — unparent for world-space orbit
            Transform tpSocket = playerRoot.Find("TP_Socket");
            if (tpSocket != null)
            {
                thirdPersonCamera = tpSocket.GetComponent<Camera>();
                if (thirdPersonCamera == null)
                    Debug.LogWarning("[PlayerController] TP_Socket missing Camera component.");
                else
                {
                    // Unparent & initial world setup
                    thirdPersonCamera.transform.SetParent(null);
                    Vector3 initOffset = startInThirdPerson ? tpOffset : aimOffset;
                    thirdPersonCamera.transform.position = playerRoot.position + playerRoot.rotation * initOffset;
                    thirdPersonCamera.transform.rotation = Quaternion.Euler(0, playerRoot.eulerAngles.y, 0);
                }
            }
            else
            {
                // Fallback
                Debug.LogWarning("[PlayerController] TP_Socket not found — creating fallback.");
                GameObject tpGo = new GameObject("TP_Camera");
                thirdPersonCamera = tpGo.AddComponent<Camera>();
                thirdPersonCamera.transform.position = playerRoot.position + playerRoot.rotation * tpOffset;
                thirdPersonCamera.transform.rotation = Quaternion.Euler(0, playerRoot.eulerAngles.y, 0);
            }

            // Initialize death camera
            GameObject deathCamGO = GameObject.Find("DeathCam");
            if (deathCamGO != null)
            {
                deathCamera = deathCamGO.GetComponent<Camera>();
                if (deathCamera == null)
                    Debug.LogWarning("[PlayerController] DeathCam GameObject missing Camera component.");
            }
            else
            {
                Debug.LogWarning("[PlayerController] DeathCam not found in scene.");
            }

            // Try find an Out socket and register it with the SocketManager (non-exclusive / additive)
            Transform outSocket = playerRoot.Find("Out_Socket") ?? playerRoot.Find("OutSocket") ?? playerRoot.Find("Out");
            if (outSocket != null)
            {
                outCamera = outSocket.GetComponent<Camera>();
                if (outCamera == null)
                {
                    Debug.LogWarning("[PlayerController] Out socket found but missing Camera component.");
                }
                else
                {
                    SocketManager.Instance?.RegisterSocket("Out", outSocket, outCamera, exclusive: false);
                }
            }

            SwitchActiveCamera();
        }

        private void SwitchActiveCamera()
        {
            // Enable active, disable inactive + set MainCamera tag
            if (firstPersonCamera)
            {
                firstPersonCamera.enabled = (currentCamMode == CamMode.FirstPerson);
                firstPersonCamera.tag = firstPersonCamera.enabled ? "MainCamera" : "Untagged";
            }
            if (thirdPersonCamera)
            {
                // ✅ Enable TP camera for BOTH ThirdPerson AND ThirdPersonAim modes (but not Death)
                thirdPersonCamera.enabled = (currentCamMode == CamMode.ThirdPerson || currentCamMode == CamMode.ThirdPersonAim);
                thirdPersonCamera.tag = thirdPersonCamera.enabled ? "MainCamera" : "Untagged";
            }
            if (deathCamera)
            {
                deathCamera.enabled = (currentCamMode == CamMode.Death);
                deathCamera.tag = deathCamera.enabled ? "MainCamera" : "Untagged";
            }
            if (outCamera)
            {
                outCamera.enabled = (currentCamMode == CamMode.Out);
                outCamera.tag = outCamera.enabled ? "MainCamera" : "Untagged";
            }

            activeCamera = currentCamMode switch
            {
                CamMode.FirstPerson => firstPersonCamera,
                CamMode.ThirdPerson or CamMode.ThirdPersonAim => thirdPersonCamera,
                CamMode.Death => deathCamera,
                CamMode.Out => outCamera,
                _ => thirdPersonCamera
            };

            // Notify SocketManager about the active camera so it can remain authoritative
            SocketManager.Instance?.SetActiveCamera(activeCamera);
        }

        private void RegisterWorldBridge()
        {
            // Toggle cam via invoker (e.g., from UI)
            WorldBridgeSystem.Instance?.RegisterInvoker("player.cam.toggle", _ =>
            {
                currentCamMode = (currentCamMode == CamMode.FirstPerson) ? CamMode.ThirdPerson : CamMode.FirstPerson;
                SwitchActiveCamera();
            });

            // UID setup (unchanged)
            var playerInstance = gameObject;
            var uid = playerInstance.GetComponent<UniqueId>() ?? playerInstance.AddComponent<UniqueId>();
            uid.isDataContainer = true;
            if (WorldBridgeSystem.Instance?.data != null)
            {
                var activeData = WorldBridgeSystem.Instance.data;
                uid.manualId = activeData switch
                {
                    LiraData lira => lira.DefaultPlayerId,
                    KinueeData kinuee => kinuee.DefaultPlayerId,
                    HosData hos => hos.DefaultPlayerId,
                    _ => $"unknown_{uid.GetID():N}"
                };
            }
            WorldBridgeSystem.Instance?.RegisterID(uid.GetID(), playerInstance);
        }


        /// <summary>
        /// Runtime Update loop for ctrlPlayer
        /// <summary>
        private bool isAimHeld; //isAimHeld. True or False. Pushcase : Popcase . Invoke 
        private void Update()
        {
            if (IsPlayerDead) //IsPlayerDead => IsDead + ragdoll
            {
                HandleDeathState(); //dead method
                return; //bail
            }

            bool aimInput = Input.GetMouseButton(1); //isAimHeld's own little bool, input right click..

            if (aimInput != isAimHeld) //toggle

            {
                isAimHeld = aimInput;

                // Tell the case controller about the aiming state 
                //this is where aimInput gets handleAiming logic
                if (isAimHeld)
                    HandleAiming();
                else
                    caseController.PopCase(PlayerCaseController.PlayerCase.Aim); //dispose the invoke case

            }

            // Toggle between FP and TPFree with V (blocked during Standby/Cinematic/UI cases) see in playerCase
            if (Input.GetKeyDown(KeyCode.V))
            {
                var currentCase = caseController.GetCurrentCase();
                if (!PlayerCaseController.IsBlockingCameraSwitch(currentCase)) //case check
                {
                    bool wasFirstPerson = currentCamMode == CamMode.FirstPerson;
                    currentCamMode = wasFirstPerson ? CamMode.ThirdPerson : CamMode.FirstPerson; //checks the mode and syncs cameras
                    SyncPitchYawOnModeSwitch(wasFirstPerson);
                    SwitchActiveCamera();
                }
                else
                {
                    Debug.Log($"[PlayerController] Camera switch blocked by case: {currentCase}");
                    //cant switch because a case invoke is blocking it
                }
            }

            // Promote to TP Aim when the button is held. Aim sway is applied and case still invoked unless input changed.
            //aim sway is based on PlayerData and is linked to PlayerStatContext (int) (float)
            if (currentCamMode == CamMode.ThirdPerson && isAimHeld)
                currentCamMode = CamMode.ThirdPersonAim;
            else if (currentCamMode == CamMode.ThirdPersonAim && !isAimHeld)
                currentCamMode = CamMode.ThirdPerson;

            // Handle dialogue FOV zoom (Oblivion-style) keep uptodate with dialogue system! maybe guard with if statement.
            UpdateDialogueFOV();

            CheckGrounded(); //normal ground check
            HandleLookInput(); //look input math
            HandleMovementInput(); //movement input math
        }

        /// <summary>
        /// isAimHeld's Logic for Aiming... True or False. Pushcase : Popcase . Invoke 
        /// adding in equipment manager logic for gun mechanics
        /// </summary>
        private void HandleAiming()
        {
            if (IsPlayerDead)
            {
                return;
            }
            else
            {

                if (isAimHeld)
                {
                    if (PlayerCaseController.AreAnyCasesActive(PlayerCaseController.PlayerCase.Standby, PlayerCaseController.PlayerCase.Dead)
                    || PlayerCaseController.AreAnyCasesActive(PlayerCaseController.PlayerCase.Cinematic, PlayerCaseController.PlayerCase.Dialogue))
                    {
                        return;
                    }
                    caseController.PushCase(PlayerCaseController.PlayerCase.Aim); //give case token for that enum Aim
                    moveSpeed = GetAimMovementSpeedModifier(); //Stat linking from PlayerStatContext and PlayerData auto marks

                    var equipmentMgr = GetComponent<EquipmentManager>();
                    if (equipmentMgr != null && equipmentMgr.HasWeapon)
                    {
                        if (!caseController.HasCase(PlayerCaseController.PlayerCase.Combat))
                            caseController.PushCase(PlayerCaseController.PlayerCase.Combat);
                    }

                    UpdateCameras();
                    Debug.Log($"[PlayerController] Entered Aim mode...");
                }
                else
                {
                    caseController.PopCase(PlayerCaseController.PlayerCase.Aim);
                    caseController.PopCase(PlayerCaseController.PlayerCase.Combat);
                    moveSpeed = 5f;
                }
            }
        }
    /// <summary>
    /// Handles dialogue FOV zoom (Oblivion-style conversation camera effect).
    /// Smoothly transitions FOV when entering/exiting Dialogue case.
    /// </summary>
    private void UpdateDialogueFOV()
    {
        if (firstPersonCamera == null) return;

        var currentCase = caseController?.GetCurrentCase() ?? PlayerCaseController.PlayerCase.None;
        float targetFOV = (currentCase == PlayerCaseController.PlayerCase.Dialogue) ? dialogueFOV : defaultFOV;

        firstPersonCamera.fieldOfView = Mathf.Lerp(
            firstPersonCamera.fieldOfView,
            targetFOV,
            Time.deltaTime * dialogueFOVLerpSpeed
        );
    }

        private void HandleDeathState()
        {
            if (!hasHandledDeath)
            {
                hasHandledDeath = true;
                OnPlayerDeath();
            }
            
            if (isAimHeld)
            {
                isAimHeld = false;
                if (caseController != null)
                {
                    caseController.PopCase(PlayerCaseController.PlayerCase.Aim);
                    caseController.PopCase(PlayerCaseController.PlayerCase.Combat);
                }
            }
            
            moveInput = Vector3.zero;
            jumpInput = false;
            
            if (rb != null)
                rb.velocity = Vector3.zero;
        }

        private void OnPlayerDeath()
        {
            if (deathCamera != null && deathCameraGO != null)
            {
                SwitchToDeathCamera();
            }
            
            var equipmentMgr = GetComponent<EquipmentManager>();
            if (equipmentMgr != null)
            {
                equipmentMgr.EnableWeaponPhysics();
            }
            
            Debug.Log("[PlayerController] Player death sequence initiated");
        }

        private void SwitchToDeathCamera()
        {
            if (deathCamera != null)
            {
                currentCamMode = CamMode.Death;
                SwitchActiveCamera();
                
                deathCameraGO.transform.LookAt(playerRoot.position + Vector3.up * 1.5f);
                Debug.Log("[PlayerController] Switched to death camera");
            }
            else
            {
                Debug.LogWarning("[PlayerController] Death camera not initialized!");
            }
        }


private void SyncPitchYawOnModeSwitch(bool wasFirstPerson)
{
    if (!playerRoot) return;
    if (!wasFirstPerson)
    {
        currentPitch = orbitPitch;
        playerRoot.rotation = Quaternion.Euler(0f, orbitYaw, 0f);
    }
    else
    {
        orbitYaw = Mathf.Repeat(playerRoot.eulerAngles.y, 360f);
        orbitPitch = currentPitch;
    }
}

        private void LateUpdate()
        {
            // 5. Camera pos/rot — post-movement, smooth
            UpdateCameras();
        }

        private void FixedUpdate()
        {
            //Block movement physics when dead
    if (IsPlayerDead)
    {
        // Stop all movement
        moveInput = Vector3.zero;
        jumpInput = false;
        
        // Optional: Stop rigidbody velocity
        if (rb != null)
            rb.velocity = Vector3.zero;
        
        return;  // Skip movement application
    }
            // 6. Physics movement — smoothed velocity, no transform hacks
            ApplyMovement();
        }

        private void HandleLookInput()
{
    if (!playerRoot || !activeCamera) return;

    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

    if (currentCamMode == CamMode.FirstPerson)
    {
        playerRoot.Rotate(Vector3.up, mouseX, Space.World);
        orbitYaw = Mathf.Repeat(playerRoot.eulerAngles.y, 360f);
        currentPitch = Mathf.Clamp(currentPitch - mouseY, pitchClampMin, pitchClampMax);
        orbitPitch = currentPitch;
        return;
    }

    orbitYaw = Mathf.Repeat(orbitYaw + mouseX, 360f);
    orbitPitch = Mathf.Clamp(orbitPitch - mouseY, pitchClampMin, pitchClampMax);

    if (currentCamMode == CamMode.ThirdPersonAim)
    {
        Quaternion target = Quaternion.Euler(0f, orbitYaw, 0f);
        playerRoot.rotation = Quaternion.Slerp(playerRoot.rotation, target, Time.deltaTime * 12f);
    }
}

private void UpdateCameras()
{
    if (!playerRoot) return;

    if (currentCamMode == CamMode.Death) return;

    if (currentCamMode == CamMode.FirstPerson && firstPersonCamera)
    {
        firstPersonCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        
        // ✅ Apply weapon sway if aiming
        if (isAimHeld)
        {
            float swayAmount = GetAimSwayAmount();
            float swaySpeed = GetAimSwaySpeed();
            
            float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            float swayY = Mathf.Cos(Time.time * swaySpeed * 0.7f) * swayAmount;
            
            firstPersonCamera.transform.localRotation *= 
                Quaternion.Euler(swayY, swayX, 0f);
        }
        return;
    }

    if (!thirdPersonCamera) return;

    Vector3 offset = currentCamMode == CamMode.ThirdPersonAim ? aimOffset : tpOffset;
    Quaternion targetRot = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
    Vector3 targetPos = playerRoot.position + targetRot * offset;

    thirdPersonCamera.transform.position = Vector3.Lerp(
        thirdPersonCamera.transform.position, targetPos, Time.deltaTime * camLerpSpeed);
    thirdPersonCamera.transform.rotation = Quaternion.Slerp(
        thirdPersonCamera.transform.rotation, targetRot, Time.deltaTime * camLerpSpeed);

    // ✅ FIX #2: Apply weapon sway in THIRD-PERSON AIM mode too
    if (currentCamMode == CamMode.ThirdPersonAim && isAimHeld)
    {
        float swayAmount = GetAimSwayAmount();
        float swaySpeed = GetAimSwaySpeed();
        
        float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(Time.time * swaySpeed * 0.7f) * swayAmount;
        
        thirdPersonCamera.transform.rotation *= Quaternion.Euler(swayY, swayX, 0f);
    }

    if (currentCamMode != CamMode.FirstPerson)
        currentPitch = orbitPitch;
}


        private void CheckGrounded()
        {
            if (!cc || !playerRoot) return;
            Vector3 rayStart = playerRoot.position - Vector3.up * (cc.height * 0.5f - cc.radius * 0.1f);
           isGrounded = characterController.isGrounded;

        }

        private void HandleMovementInput()
        {
            if (!activeCamera) return;

            // View-relative — flat plane
            Vector3 camForward = Vector3.ProjectOnPlane(activeCamera.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(activeCamera.transform.right, Vector3.up).normalized;

            


            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            moveInput = (h * camRight + v * camForward).normalized;
            //moveInput = (h * camRight + v * camForward);
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
            jumpInput = Input.GetKeyDown(KeyCode.Space) && isGrounded;
        }
            private void OnEnable()
            {
            // Automatically assign a valid camera on reactivation
            if (activeCamera == null)
            activeCamera = Camera.main ?? GetComponentInChildren<Camera>();
            }



        // private void CheckGrounded()
        // {
        //      Vector3 start = transform.position + Vector3.up * 0.1f;
        //     Vector3 end = start + Vector3.down * (cc.height - cc.radius);
        //      isGrounded = Physics.CheckCapsule(start, end, cc.radius * 0.9f, groundMask);
        //     Debug.DrawLine(start, end, isGrounded ? Color.green : Color.red);
        // }
        private Vector3 verticalVelocity;

        private void ApplyMovement()
        {
            if (!characterController) return;

            float speed = (caseController.HasCase(PlayerCaseController.PlayerCase.Aim)) ? aimMoveSpeed : moveSpeed;
            Vector3 move = moveInput * speed;

            // Handle gravity
            if (characterController.isGrounded)
            {
                verticalVelocity.y = -1f; // small downward force to keep grounded
                if (jumpInput)
                {
                    verticalVelocity.y = jumpForce;
                    jumpInput = false;
                }
            }
            else
            {
                verticalVelocity += Physics.gravity * Time.deltaTime;
            }




            Vector3 horizontalVelocity = new Vector3(move.x, 0, move.z);
            Vector3 finalVelocity = move + new Vector3(0, verticalVelocity.y, 0);
            characterController.Move(finalVelocity * Time.deltaTime);

            if (horizontalVelocity.sqrMagnitude > 0.01f
            && currentCamMode == CamMode.ThirdPerson)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalVelocity.normalized);
                playerRoot.rotation = Quaternion.Slerp(playerRoot.rotation, targetRot, Time.fixedDeltaTime * 10f);
            }


        }

#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    if (!playerRoot) return;

    Gizmos.color = isGrounded ? Color.green : Color.red;
    Gizmos.DrawWireSphere(playerRoot.position - Vector3.up * (cc.height * 0.5f - cc.radius * 0.1f), 0.1f);

    // Visualize camera offsets
    Gizmos.color = Color.cyan;
    Gizmos.DrawLine(playerRoot.position, playerRoot.position + playerRoot.rotation * tpOffset);
    Gizmos.DrawSphere(playerRoot.position + playerRoot.rotation * tpOffset, 0.05f);


}

private void HandleStatsChanged(object sender, PlayerDataContainer data)
    {
        Debug.Log($"[HUD] HP: {data.currentHP}/{data.maxHP} | Level: {data.level}");
        // Update your UI labels here
    }
#endif
#if UNITY_EDITOR
[Header("Debug Overlay")]
[SerializeField] private bool showDebugOverlay = true;
private GUIStyle debugStyle;

private void OnGUI()
{
    if (!showDebugOverlay) return;

    if (debugStyle == null)
    {
        debugStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };
    }
    
    // Box placement — top left

  
    
    GUILayout.BeginArea(new Rect(20, 50, 200, 500), GUI.skin.window);
    GUILayout.Label($"<b>🧍 Player Debug</b>", debugStyle);
    GUILayout.Label($"Cam Mode: {currentCamMode}", debugStyle);
    GUILayout.Label($"Active Cam: {(activeCamera ? activeCamera.name : "None")}", debugStyle);
    GUILayout.Label($"Grounded: {(isGrounded ? "✅ Yes" : "❌ No")}", debugStyle);
    GUILayout.Label($"XP: {activeData.xp}", debugStyle);
    GUILayout.Label($"Name: {activeData.DefaultDisplayName}", debugStyle);
    GUILayout.Label($"HP: {activeData.currentHP}/{activeData.maxHP}", debugStyle);
    GUILayout.Label($"Level: {activeData.level}", debugStyle);
    GUILayout.Label($"Velocity: {characterController.velocity.magnitude:F2}", debugStyle);
    GUILayout.Label($"Pitch: {currentPitch:F1}", debugStyle);
    GUILayout.Label($"Root: {(playerRoot ? playerRoot.name : "null")}", debugStyle);
    GUILayout.EndArea();
    
}
#endif

            


    }
    }


      
        
    