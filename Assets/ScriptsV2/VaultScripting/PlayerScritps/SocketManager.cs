using System;
using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Invoker;

namespace VaultSystems.Controllers
{
    /// <summary>
    /// Manages camera socket switching based on PlayerCase state.
    /// Enables/disables camera sockets (FP_Socket, TP_Socket, Dialogue_Socket, Shop_Socket, etc.)
    /// based on the active player case.
    /// 
    /// Socket hierarchy:
    /// - FP_Socket: First-person camera (parented to player head)
    /// - TP_Socket: Third-person camera (world-space orbit)
    /// - Dialogue_Socket: Dialogue/conversation camera (fixed FOV zoom)
    /// - Shop_Socket: Weapon shop camera (isometric/counter view)
    /// </summary>
    [DefaultExecutionOrder(26)] // After PlayerCaseController (25)
    public class SocketManager : MonoBehaviour
    {
        // Simple singleton for easy access from PlayerController
        public static SocketManager Instance { get; private set; }

        // Expose the currently active camera for other systems
        public Camera ActiveCamera { get; private set; }

        // Camera mode enum for clearer API
        public enum CamMode
        {
            FP,
            TP,
            Dialogue,
            Shop,
            Unknown
        }
        [Header("Socket References")]
        [SerializeField] private Transform fpSocket;
        [SerializeField] private Transform tpSocket;
        [SerializeField] private Transform dialogueSocket;
        [SerializeField] private Transform shopSocket;

        private PlayerCaseController caseController;
        private Camera fpCamera;
        private Camera tpCamera;
        private Camera dialogueCamera;
        private Camera shopCamera;

        private PlayerCaseController.PlayerCase lastCase = PlayerCaseController.PlayerCase.None;

        // Simple registration for dynamic/additive sockets (keyed by string id)
        private class SocketEntry
        {
            public Transform socket;
            public Camera camera;
            public bool exclusive;
        }

        private readonly Dictionary<string, SocketEntry> registeredSockets = new Dictionary<string, SocketEntry>(StringComparer.OrdinalIgnoreCase);

        private void Start()
        {
            // establish singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple SocketManager instances detected. Using first instance.");
            }
            else
            {
                Instance = this;
            }

            caseController = PlayerCaseController.Instance;
            if (caseController == null)
            {
                Debug.LogError("[SocketManager] PlayerCaseController not found!");
                return;
            }

            // Cache cameras from sockets
            CacheCameras();
        }

        private void CacheCameras()
        {
            if (fpSocket != null)
                fpCamera = fpSocket.GetComponent<Camera>();
            if (tpSocket != null)
                tpCamera = tpSocket.GetComponent<Camera>();
            if (dialogueSocket != null)
                dialogueCamera = dialogueSocket.GetComponent<Camera>();
            if (shopSocket != null)
                shopCamera = shopSocket.GetComponent<Camera>();

            // Ensure ActiveCamera is consistent (prefer previously set, otherwise pick a default)
            if (ActiveCamera == null)
                ActiveCamera = fpCamera ?? tpCamera ?? Camera.main;

            // Register built-in sockets so other systems can query them by id
            RegisterSocket("FP", fpSocket, fpCamera, exclusive: true);
            RegisterSocket("TP", tpSocket, tpCamera, exclusive: false);
            RegisterSocket("Dialogue", dialogueSocket, dialogueCamera, exclusive: true);
            RegisterSocket("Shop", shopSocket, shopCamera, exclusive: true);
        }

        private void Update()
        {
            if (caseController == null) return;

            var currentCase = caseController.GetCurrentCase();
            if (currentCase == lastCase) return; // No change

            lastCase = currentCase;
            UpdateSockets(currentCase);
        }

        /// <summary>
        /// Enable/disable sockets based on the active case.
        /// </summary>
        private void UpdateSockets(PlayerCaseController.PlayerCase activeCase)
        {
            switch (activeCase)
            {
                case PlayerCaseController.PlayerCase.Standby:
                    // Standby mode: disable all camera input, lock to current socket
                    // (Camera switching is blocked by IsBlockingCameraSwitch check in PlayerController)
                    DisableAllSockets();
                    break;

                case PlayerCaseController.PlayerCase.Dialogue:
                    // Dialogue mode: swap to dialogue socket (Oblivion-style zoom)
                    // Note: PlayerController handles FOV zoom, this just swaps the socket
                    EnableOnlySocket(dialogueSocket, dialogueCamera);
                    break;

                case PlayerCaseController.PlayerCase.UI:
                    // UI mode: keep current socket active but disable input
                    // (Handled by PlayerController input checks)
                    break;

                default:
                    // All other cases: enable standard FP/TP sockets (PlayerController manages the active one)
                    // This is a fallback; PlayerController normally controls this
                    break;
            }
        }

        private void DisableAllSockets()
        {
            DisableSocket(fpSocket, fpCamera);
            DisableSocket(tpSocket, tpCamera);
            DisableSocket(dialogueSocket, dialogueCamera);
            DisableSocket(shopSocket, shopCamera);
        }

        private void EnableOnlySocket(Transform socket, Camera camera)
        {
            // Disable all except target
            if (socket != fpSocket) DisableSocket(fpSocket, fpCamera);
            if (socket != tpSocket) DisableSocket(tpSocket, tpCamera);
            if (socket != dialogueSocket) DisableSocket(dialogueSocket, dialogueCamera);
            if (socket != shopSocket) DisableSocket(shopSocket, shopCamera);

            // Enable target
            if (socket != null && camera != null)
            {
                socket.gameObject.SetActive(true);
                camera.enabled = true;
                camera.tag = "MainCamera";
                ActiveCamera = camera; // update active camera
            }
        }

        private void DisableSocket(Transform socket, Camera camera)
        {
            if (socket != null)
                socket.gameObject.SetActive(false);
            if (camera != null)
            {
                camera.enabled = false;
                camera.tag = "Untagged";
                if (ActiveCamera == camera)
                    ActiveCamera = null;
            }
        }

        /// <summary>
        /// Get a camera by CamMode (returns null if not assigned)
        /// </summary>
        public Camera GetCameraForMode(CamMode mode)
        {
            return mode switch
            {
                CamMode.FP => fpCamera,
                CamMode.TP => tpCamera,
                CamMode.Dialogue => dialogueCamera,
                CamMode.Shop => shopCamera,
                _ => ActiveCamera
            };
        }

        /// <summary>
        /// Register an arbitrary socket by id so other systems can query or enable it.
        /// id is case-insensitive. If a socket was previously registered with the same id it will be overwritten.
        /// </summary>
        public void RegisterSocket(string id, Transform socket, Camera camera, bool exclusive = true)
        {
            if (string.IsNullOrEmpty(id)) return;
            registeredSockets[id] = new SocketEntry { socket = socket, camera = camera, exclusive = exclusive };
        }

        /// <summary>
        /// Unregister a previously registered socket.
        /// </summary>
        public void UnregisterSocket(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            registeredSockets.Remove(id);
        }

        /// <summary>
        /// Get a Camera by a registration id (null if not found)
        /// </summary>
        public Camera GetCameraById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return registeredSockets.TryGetValue(id, out var e) ? e.camera : null;
        }

        /// <summary>
        /// Enable a registered socket by id. If the socket is exclusive it will disable other exclusive sockets.
        /// </summary>
        public void EnableSocketById(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!registeredSockets.TryGetValue(id, out var entry) || entry == null) return;

            if (entry.exclusive)
            {
                // disable other exclusive sockets
                foreach (var kv in registeredSockets)
                {
                    if (kv.Key.Equals(id, StringComparison.OrdinalIgnoreCase)) continue;
                    var other = kv.Value;
                    if (other != null && other.exclusive)
                        DisableSocket(other.socket, other.camera);
                }
            }

            if (entry.socket != null) entry.socket.gameObject.SetActive(true);
            if (entry.camera != null)
            {
                entry.camera.enabled = true;
                entry.camera.tag = "MainCamera";
                ActiveCamera = entry.camera;
            }
        }

        /// <summary>
        /// Disable a registered socket by id.
        /// </summary>
        public void DisableSocketById(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!registeredSockets.TryGetValue(id, out var entry) || entry == null) return;
            DisableSocket(entry.socket, entry.camera);
        }

        /// <summary>
        /// External sets the active camera reference (keeps SocketManager decoupled from camera math)
        /// </summary>
        public void SetActiveCamera(Camera cam)
        {
            ActiveCamera = cam;
        }

        /// <summary>
        /// Swap to a camera by mode (wrapper around SwapToSocket)
        /// </summary>
        public void SwapToCamMode(CamMode mode)
        {
            switch (mode)
            {
                case CamMode.FP: SwapToSocket(fpSocket, fpCamera); break;
                case CamMode.TP: SwapToSocket(tpSocket, tpCamera); break;
                case CamMode.Dialogue: SwapToSocket(dialogueSocket, dialogueCamera); break;
                case CamMode.Shop: SwapToSocket(shopSocket, shopCamera); break;
                default: break;
            }
        }

        /// <summary>
        /// External call to manually enable shop mode.
        /// Usage: SocketManager.Instance?.EnableShopMode();
        /// </summary>
        public void EnableShopMode()
        {
            var caseCtrl = PlayerCaseController.Instance;
            if (caseCtrl != null)
                caseCtrl.PushCase(PlayerCaseController.PlayerCase.Standby);
            // Then EnterShop() in your shop manager swaps to Shop_Socket
            SwapToSocket(shopSocket, shopCamera);
        }

        /// <summary>
        /// External call to exit shop mode.
        /// </summary>
        public void ExitShopMode()
        {
            var caseCtrl = PlayerCaseController.Instance;
            if (caseCtrl != null)
                caseCtrl.PopCase(PlayerCaseController.PlayerCase.Standby);
        }

        /// <summary>
        /// Utility to swap to a specific socket (for custom scenarios).
        /// </summary>
        public void SwapToSocket(Transform socket, Camera camera)
        {
            EnableOnlySocket(socket, camera);
        }

        /// <summary>
        /// Auto-find sockets from player root if not assigned.
        /// Call this if you haven't manually assigned sockets in inspector.
        /// </summary>
        public void AutoDiscoverSockets()
        {
            Transform playerRoot = transform.parent ?? transform;

            if (fpSocket == null)
                fpSocket = playerRoot.Find("FP_Socket");
            if (tpSocket == null)
                tpSocket = playerRoot.Find("TP_Socket");
            if (dialogueSocket == null)
                dialogueSocket = playerRoot.Find("Dialogue_Socket");
            if (shopSocket == null)
                shopSocket = playerRoot.Find("Shop_Socket");

            CacheCameras();
        }
    }
}