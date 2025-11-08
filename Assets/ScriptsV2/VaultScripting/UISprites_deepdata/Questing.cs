using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prototype shared subquest for a single static marker.
/// Tracks player distance, triggers simple actions, updates UI, and auto-registers with Compass.
/// </summary>
public class SubquestRuntime : MonoBehaviour
{
    [Header("Subquest Settings")]
    public QuestMarker questMarker;           // Assign the marker in inspector
    public float triggerDistance = 2f;        // Distance to activate subquest
   // public Animator playerAnimator;           // Optional animation trigger
   // public GameObject inventoryItemPrefab;    // Optional item spawn

    [Header("UI")]
    public Text subquestText;                 // UI text to display subquest status

    [Header("Debug")]
    public bool debugLogs = true;

    private bool isCompleted = false;
    private Compass compass;

    private void Start()
    {
        // Find Compass dynamically and register marker
        compass = FindObjectOfType<Compass>();
        if (compass != null && questMarker != null)
        {
            compass.AddQuestMarker(questMarker);
            if (debugLogs) Debug.Log($"[Subquest] Marker '{questMarker.name}' registered with Compass.");
        }

        // Initialize UI
        if (subquestText != null)
            subquestText.text = $"Subquest: Go to {questMarker.name}";
    }

    private void Update()
    {
        if (isCompleted || questMarker == null || GameManager.Instance?.playerTransform == null)
            return;

        Vector2 playerPos = new Vector2(GameManager.Instance.playerTransform.position.x,
                                        GameManager.Instance.playerTransform.position.z);
        Vector2 markerPos = questMarker.position;

        float distance = Vector2.Distance(playerPos, markerPos);

        // Optional: update UI distance
        if (subquestText != null)
            subquestText.text = $"Subquest: {questMarker.name} ({distance:F1}m)";

        if (distance <= triggerDistance)
            CompleteSubquest();
    }

    private void CompleteSubquest()
    {
        isCompleted = true;

        if (debugLogs) Debug.Log($"[Subquest] Marker '{questMarker.name}' reached. Subquest complete!");

        // Update UI
        if (subquestText != null)
            subquestText.text = $"Subquest Complete: {questMarker.name}!";

        // Optional: play animation
        //if (playerAnimator != null)
        //    playerAnimator.SetTrigger("Interact");

        // Optional: spawn item
      //  if (inventoryItemPrefab != null && GameManager.Instance?.playerTransform != null)
     //   {
     //       Vector3 spawnPos = GameManager.Instance.playerTransform.position + GameManager.Instance.playerTransform.forward;
     //       Instantiate(inventoryItemPrefab, spawnPos, Quaternion.identity);
    //    }
    }

    /// <summary>
    /// Reset subquest for testing purposes
    /// </summary>
    public void ResetSubquest()
    {
        isCompleted = false;

        if (subquestText != null)
            subquestText.text = $"Subquest: Go to {questMarker.name}";
    }
}
