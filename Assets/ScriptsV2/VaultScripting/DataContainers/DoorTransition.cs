using UnityEngine;
using VaultSystems.Data;
using System.Collections.Generic;
using UnityEngine.UI;
using VaultSystems.Controllers;
namespace VaultSystems.World
{
    public class DoorTransition : MonoBehaviour
    {
        [System.Serializable]
        public class DoorCondition
        {
            public enum ConditionType { Quest, Locked, Custom }
            
            public ConditionType type;
            public string questId;
            public string customKey;
        }

        [Header("Destination")]
        public string targetSceneName = "Village";
        public string targetSpawnId = "player_default";
        
        [Header("Door Settings")]
        public bool requiresInteraction = true;
        public KeyCode interactKey = KeyCode.E;
        
        [Header("Door Conditions")]
        public List<DoorCondition> conditions = new();
        
        [Header("UI")]
        public GameObject interactionPrompt;
        public GameObject conditionBlockedUI;
        public Text blockedReasonText;
        
        private bool playerInTrigger = false;
        private PlayerController playerController;
        private PlayerDataContainer playerData;
        private float blockedMessageTime = 0f;

        private void OnTriggerEnter(Collider other)
        {
            playerController = other.GetComponent<PlayerController>();
            playerData = other.GetComponent<PlayerDataContainer>();
            
            if (playerController != null)
            {
                playerInTrigger = true;
                ShowInteractionPrompt();
                Debug.Log($"[Door] Player near door to '{targetSceneName}'");
                
                if (!requiresInteraction)
                    TryEnterDoor();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                playerInTrigger = false;
                HideInteractionPrompt();
            }
        }

        private void Update()
        {
            if (playerInTrigger && requiresInteraction && Input.GetKeyDown(interactKey))
            {
                TryEnterDoor();
            }

            if (conditionBlockedUI != null && conditionBlockedUI.activeSelf)
            {
                blockedMessageTime -= Time.deltaTime;
                if (blockedMessageTime <= 0f)
                    HideBlockedMessage();
            }
        }

        public void TryEnterDoor()
        {
            if (!CanEnterDoor(out string reason))
            {
                ShowBlockedMessage(reason);
                Debug.LogWarning($"[Door] Cannot enter: {reason}");
                return;
            }

            TransitionToDoor();
        }

        public bool CanEnterDoor(out string reason)
        {
            reason = string.Empty;
            
            if (playerData == null)
            {
                reason = "Player data not found";
                return false;
            }

            foreach (var condition in conditions)
            {
                if (!CheckCondition(condition, out string conditionReason))
                {
                    reason = conditionReason;
                    return false;
                }
            }

            return true;
        }

        private bool CheckCondition(DoorCondition condition, out string reason)
        {
            reason = string.Empty;

            switch (condition.type)
            {
                case DoorCondition.ConditionType.Locked:
                    reason = "Door is locked";
                    return false;

                case DoorCondition.ConditionType.Quest:
                    if (!playerData.completedSubquests.Contains(condition.questId))
                    {
                        reason = $"Quest '{condition.questId}' required";
                        return false;
                    }
                    break;

                case DoorCondition.ConditionType.Custom:
                    if (!CheckCustomCondition(condition.customKey))
                    {
                        reason = $"Condition '{condition.customKey}' not met";
                        return false;
                    }
                    break;
            }

            return true;
        }

        protected virtual bool CheckCustomCondition(string key)
        {
            return true;
        }

        public void AddCondition(DoorCondition.ConditionType type, string value = "")
        {
            conditions.Add(new DoorCondition 
            { 
                type = type, 
                questId = type == DoorCondition.ConditionType.Quest ? value : "",
                customKey = type == DoorCondition.ConditionType.Custom ? value : ""
            });
            Debug.Log($"[Door] Added condition: {type} ({value})");
        }

        public void RemoveCondition(DoorCondition condition)
        {
            conditions.Remove(condition);
            Debug.Log($"[Door] Removed condition");
        }

        public void RemoveConditionByType(DoorCondition.ConditionType type)
        {
            conditions.RemoveAll(c => c.type == type);
            Debug.Log($"[Door] Removed all {type} conditions");
        }

        public void ClearConditions()
        {
            conditions.Clear();
            Debug.Log("[Door] All conditions cleared");
        }

        public bool HasCondition(DoorCondition.ConditionType type)
        {
            return conditions.Exists(c => c.type == type);
        }

        public void LockDoor()
        {
            if (!HasCondition(DoorCondition.ConditionType.Locked))
                AddCondition(DoorCondition.ConditionType.Locked);
        }

        public void UnlockDoor()
        {
            RemoveConditionByType(DoorCondition.ConditionType.Locked);
        }

        private void TransitionToDoor()
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning("[Door] Target scene not set!");
                return;
            }

            playerInTrigger = false;
            HideInteractionPrompt();
            
            if (playerData != null)
            {
                playerData.lastSpawnPointId = targetSpawnId;
                playerData.lastSpawnPointScene = targetSceneName;
                playerData.MarkDirty();
            }

            Debug.Log($"[Door] Transitioning to {targetSceneName} @ spawn '{targetSpawnId}'");
            
            StreamingCellManager.Instance?.EnterCell(targetSceneName, OnSceneLoadComplete);
        }

        private void OnSceneLoadComplete()
        {
            Debug.Log($"[Door] Arrived at {targetSceneName}");
        }

        private void ShowInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }

        private void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        private void ShowBlockedMessage(string reason)
        {
            if (conditionBlockedUI != null)
            {
                blockedMessageTime = 3f;
                conditionBlockedUI.SetActive(true);
                
                if (blockedReasonText != null)
                    blockedReasonText.text = reason;
            }
        }

        private void HideBlockedMessage()
        {
            if (conditionBlockedUI != null)
                conditionBlockedUI.SetActive(false);
        }
    }
}
