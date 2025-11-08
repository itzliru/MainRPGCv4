using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Data;
public class AmmoReplenish : MonoBehaviour
{
    // Start is called before the first frame update
private void OnTriggerEnter(Collider other)
{
    var npcData = other.GetComponent<NPCDataContainer>();
    if (npcData != null && npcData.equippedWeaponData != null)
    {
         Debug.Log($"Refilling ammo for {npcData.displayName}");
        npcData.ReplenishAmmo(npcData.equippedWeaponData.ammo); // full refill
        Destroy(gameObject);
    }
}


}
