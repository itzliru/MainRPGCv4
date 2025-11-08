using UnityEngine;
using VaultSystems.Controllers;
using VaultSystems.Errors;
using VaultSystems.Invoker;
using System;

namespace VaultSystems.Errors
{
    /// <summary>
    /// Do not use this system its not yet implemented! it can break the game if used!
    /// </summary>
    
    public static class VaultErrorPauseController
    {
        private static bool isPausedByError = false;

        public static void HandleErrorPause(VaultErrorType type)
        {
            if (type == VaultErrorType.Breakpoint || type == VaultErrorType.Assertion || 
                type == VaultErrorType.Critical || type == VaultErrorType.Primitive)
            {
                Action pause = () =>
                {
                    if (isPausedByError) return;
                    isPausedByError = true;
                    Time.timeScale = 0f;
                    Debug.Log("[VaultErrorPauseController] Game paused due to Vault error.");

                    var caseController = UnityEngine.Object.FindObjectOfType<PlayerCaseController>();
                    if (caseController != null)
                        caseController.PushCase(PlayerCaseController.PlayerCase.Pause);
                };
                pause();
            }
        }

        public static void ResumeGame()
        {
            if (!isPausedByError) return;
            isPausedByError = false;
            Time.timeScale = 1f;
            Debug.Log("[VaultErrorPauseController] Game resumed.");

            var caseController = UnityEngine.Object.FindObjectOfType<PlayerCaseController>();
            if (caseController != null)
                caseController.PopCase(PlayerCaseController.PlayerCase.Pause);
        }
    }
}
