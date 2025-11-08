using UnityEngine;
using VaultSystems.World;
using VaultSystems.Data;
using VaultSystems.Controllers;
public class CustomDoorTransition : DoorTransition
{
    protected override bool CheckCustomCondition(string key)
    {
        switch (key)
        {
            //case "has_key":
              //  return PlayerInventory.Instance?.HasItem("golden_key") ?? false;
            
            case "level_10_or_higher":
                var playerData = GetComponent<PlayerDataContainer>();
                return playerData != null && playerData.level >= 10;
            
            case "faction_member":
                return GetComponent<PlayerDataContainer>()?.IsInFaction("guild") ?? false;
            
            default:
                return true;
        }
    }
}
