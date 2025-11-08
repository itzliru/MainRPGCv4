using UnityEngine;
using VaultSystems.Data;
[DefaultExecutionOrder(5)]
public class PlayerStatsLinker : MonoBehaviour
{ 
    private PlayerDataContainer linkedData;
    private bool appliedOnce = false; // Prevent multiple applications
    public static PlayerDataContainer activeData { get; private set; }
    public void Initialize(PlayerDataContainer data)
    {
        linkedData = data;

        // Only apply once when data is first set
        if (!appliedOnce)
        {
            ApplyFromData();
            appliedOnce = true;
        }
    }

    private void ApplyFromData()
    {
        if (linkedData == null) return;

        Debug.Log($"[StatsLinker] Applying data for {linkedData.displayName} | Outfit {linkedData.outfitIndex}");
        ApplyOutfitIndex(linkedData.outfitIndex);
    }

    private void ApplyOutfitIndex(int index)
    {
        Debug.Log($"[StatsLinker] Applying outfit index {index} to {linkedData.displayName}");
    }

    public void PushToData()
    {
        if (linkedData == null) return;

        linkedData.currentHP = Mathf.Clamp(linkedData.currentHP, 0, linkedData.maxHP);
        linkedData.MarkDirty();
        Debug.Log($"[StatsLinker] Updated data container for {linkedData.displayName}.");
    }

    public void ResetDataToDefaults()
    {
        if (linkedData is IResettablePlayer resettable)
        {
            resettable.ResetCharacter();
            ApplyFromData();
            Debug.Log($"[StatsLinker] {linkedData.displayName} reset to defaults.");
        }
    }
}
