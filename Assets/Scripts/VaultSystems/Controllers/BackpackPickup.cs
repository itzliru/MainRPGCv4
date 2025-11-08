using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using VaultSystems.Invoker;

public class BackpackPickupIK : MonoBehaviour
{
    [Header("Backpack References")]
    [SerializeField] private GameObject backpackWorldVersion; // Inventory_Sack_WorldVersion
    [SerializeField] private Transform worldBackpackSocket;   // Grab point on world backpack
    private GameObject backpackPlayerVersion;                 // Inventory_Sack (player's back)

    [Header("Settings")]
    [SerializeField] private float pickupDuration = 2.0f;
    [SerializeField] private string grabAnimationTrigger = "GrabBackpack";

    private PlayerAnimator1 playerAnimator;
    private bool inRange;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerAnimator = other.GetComponent<PlayerAnimator1>();
            inRange = playerAnimator != null;

            if (inRange)
                Debug.Log("[BackpackPickupIK] Backpack in range!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = false;
            playerAnimator = null;
        }
    }

    private void Update()
    {
        if (inRange && Input.GetKeyDown(KeyCode.E))
        {
            StartBackpackPickup();
        }
    }

    private void StartBackpackPickup()
    {
        if (playerAnimator == null) return;
        //invoke method to initalizebackpackik.


        playerAnimator.InitializeBackpackIK(worldBackpackSocket);
        playerAnimator.SetBackpackIKActive(true);

        // Setup backpack IK target at world socket


        // Play grab animation
        playerAnimator.PlayTrigger(grabAnimationTrigger);
        Debug.Log("[BackpackPickupIK] Starting pickup sequence...");

        // Use InvokeUpdate to lerp IK for the duration, then auto-cleanup
        IDisposable ikToken = PlayerCaseController.Instance.InvokeUpdate(
            () => playerAnimator.UpdateBackpackIKToSocket(),
            pickupDuration
        );

        // When complete, finalize the pickup
        // (Schedule finalization after duration via InvokeMethod with delay)
        PlayerCaseController.Instance.InvokeMethod(() =>
        {
            EquipBackpack();
            playerAnimator.SetBackpackIKActive(false);
            Debug.Log("[BackpackPickupIK] Backpack equipped!");
        });
        ikToken = null;
    }

    private void EquipBackpack()
    {
        // Ensure we have a valid player animator
        if (playerAnimator == null)
        {
            Debug.LogWarning("[Backpack] No playerAnimator reference found!");
            return;
        }

        // Try to find the socket
        Transform socket = playerAnimator.transform.Find("BackSocket");
        if (socket == null)
        {
            Debug.LogWarning("[Backpack] No 'BackSocket' found under player!");
            return;
        }

        // Find or cache the player’s backpack model
        if (backpackPlayerVersion == null)
        {
            backpackPlayerVersion = socket.Find("Inventory_Sack")?.gameObject;

            // If not found under socket, try globally
            if (backpackPlayerVersion == null)
                backpackPlayerVersion = GameObject.Find("Inventory_Sack");
        }

        // ✅ Safety check
        if (backpackPlayerVersion == null)
        {
            Debug.LogWarning("[Backpack] Could not find 'Inventory_Sack' object!");
            return;
        }

        // Re-parent to socket and align perfectly
        backpackPlayerVersion.transform.SetParent(socket);
        backpackPlayerVersion.transform.localPosition = Vector3.zero;
        backpackPlayerVersion.transform.localRotation = Quaternion.identity;

        // ✅ Toggle visibility
        backpackPlayerVersion.SetActive(true);

        // Hide the world version if it exists
        if (backpackWorldVersion != null)
            backpackWorldVersion.SetActive(false);

        Debug.Log("[Backpack] Equipped successfully — attached to BackSocket!");
    }
}
