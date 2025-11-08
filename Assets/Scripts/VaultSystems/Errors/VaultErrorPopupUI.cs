using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VaultSystems.Controllers;
using VaultSystems.Data;
using VaultSystems.Errors;
using System;
using System.Linq;
namespace VaultSystems.Errors
{

    /// <summary>
    /// Do not use this system its not yet implemented! it can break the game if used!
    /// </summary>
    public class VaultErrorPopupUI : MonoBehaviour
    {
        public TMP_Text titleText;
        public TMP_Text bodyText;
        public Button resetButton;
        public Button disableButton;
        public Button deleteButton;
        public Button cancelButton;
        public Image background;

        private static VaultErrorPopupUI instance;
        private VaultError currentError;

        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.SetActive(false);
        }

        public static void ShowPopup(VaultError error)
        {
            if (!instance)
            {
                var prefab = Resources.Load<VaultErrorPopupUI>("VaultErrorPopupUI");
                if (prefab == null)
                {
                    VaultBreakpoint.Critical("VaultErrorPopupUI prefab not found in Resources", null);
                    return;
                }
                instance = Instantiate(prefab);
            }

            instance.currentError = error;
            instance.titleText.text = $"[Error #{error.errorID}] {error.errorType} (ID: {error.uniqueId})";
            instance.bodyText.text = $"{error.message}\n\n{error.ToFormattedTrace()}";
            instance.background.color = instance.GetColorForType(error.errorType);
            instance.gameObject.SetActive(true);
        }

        public void OnResetPressed()
        {
            if (currentError?.context == null)
            {
                Debug.LogWarning($"[VaultErrorPopup] No context to reset for error #{currentError.errorID}");
                return;
            }

            Action reset = () =>
            {
               VaultErrorPauseController.ResumeGame();
                var ctx = currentError.context;
                var obj = ctx as GameObject ?? (ctx as Component)?.gameObject;


                if (obj != null)
                {
                    var pos = obj.transform.position;
                    var rot = obj.transform.rotation;
                    var parent = obj.transform.parent;
                    var clone = Instantiate(obj, pos, rot, parent);
                    clone.name = $"{obj.name}_ResetInstance";

                    if (obj.GetComponent<PlayerController>() != null && clone.GetComponent<PlayerController>() != null)
                    {
                        var oldPC = obj.GetComponent<PlayerController>();
                        var newPC = clone.GetComponent<PlayerController>();
                        newPC.activeCamera = oldPC.activeCamera;
                    }

                    Destroy(obj);
                    Debug.Log($"[VaultErrorPopup] Reset & reinstantiated {obj.name}");
                }
                gameObject.SetActive(false);
            };
            reset();
        }

        public void OnDisablePressed()
        {
            if (currentError?.context == null) return;
            Action disable = () =>
            {
                VaultErrorPauseController.ResumeGame();
                var ctx = currentError.context;
                var obj = ctx as GameObject ?? (ctx as Component)?.gameObject;
                if (obj)
                {
                    obj.SetActive(false);
                    Debug.Log($"[VaultErrorPopup] Disabled {obj.name}");
                }
                gameObject.SetActive(false);
            };
            disable();
        }

        public void OnDeletePressed()
        {
            if (currentError?.context == null) return;
            Action delete = () =>
            {
                VaultErrorPauseController.ResumeGame();
                var ctx = currentError.context;
                var obj = ctx as GameObject ?? (ctx as Component)?.gameObject;
                if (obj)
                {
                    Destroy(obj);
                    Debug.Log($"[VaultErrorPopup] Deleted {obj.name}");
                }
                VaultErrorDispatcher.ClearError(currentError);
                gameObject.SetActive(false);
            };
            delete();
        }

        public void OnCancelPressed()
        {
            Action cancel = () =>
            {
                VaultErrorPauseController.ResumeGame();
                gameObject.SetActive(false);
            };
            cancel();
        }

        private Color GetColorForType(VaultErrorType type)
        {
            return type switch
            {
                VaultErrorType.Breakpoint => new Color(0.4f, 0.7f, 1f),
                VaultErrorType.Assertion => new Color(0.6f, 0.9f, 0.6f),
                VaultErrorType.Runtime => new Color(1f, 0.9f, 0.5f),
                VaultErrorType.Logic => new Color(1f, 0.6f, 0.3f),
                VaultErrorType.Data => new Color(0.8f, 0.5f, 1f),
                VaultErrorType.Critical => new Color(1f, 0.3f, 0.3f),
                VaultErrorType.Primitive => new Color(0f, 1f, 1f),
                _ => Color.gray
            };
        }
    }
}