using UnityEngine;
using VaultSystems.Data;
public class TestNPCScene : MonoBehaviour
{
    public NPCDataContainer testNPCPrefab;
    private NPCDataContainer testNPCInstance;

    [Header("Disposition Test Settings")]
    public float testDelta = 35f; // Change amount per key press
    public KeyCode increaseKey = KeyCode.T;
    public KeyCode decreaseKey = KeyCode.Y;

    private void Start()
    {
        if (testNPCPrefab == null)
        {
            Debug.LogError("[TestNPCScene] Please assign a test NPC prefab.");
            return;
        }

        // Clone the NPCDataContainer for testing
        GameObject npcGO = Instantiate(testNPCPrefab.gameObject, Vector3.zero, Quaternion.identity);
        testNPCInstance = npcGO.GetComponent<NPCDataContainer>();

        if (testNPCInstance == null)
        {
            Debug.LogError("[TestNPCScene] Cloned prefab missing NPCDataContainer!");
            return;
        }

        // Make sure the combat controller is synced
        testNPCInstance.UpdateCombatStateFromData();
    }

    private void Update()
    {
        if (testNPCInstance == null) return;

        if (Input.GetKeyDown(increaseKey))
        {
            testNPCInstance.ModifyDisposition(testDelta);
            Debug.Log($"[Test] Increased disposition to {testNPCInstance.disposition}");
        }

        if (Input.GetKeyDown(decreaseKey))
        {
            testNPCInstance.ModifyDisposition(-testDelta);
            Debug.Log($"[Test] Decreased disposition to {testNPCInstance.disposition}");
        }
    }
}
