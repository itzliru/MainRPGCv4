using UnityEngine;
using System.Collections.Generic;

public class EquipmentManager : MonoBehaviour
{
    [SerializeField] private GunController gunController;
    [SerializeField] private GunScriptableObject[] weaponsByType = new GunScriptableObject[8];

    private GunType currentEquippedType;
    private Dictionary<GunType, GunScriptableObject> weaponMap;

    private void Awake()
    {
        if (gunController == null)
            gunController = GetComponent<GunController>();

        BuildWeaponMap();
    }

    private void BuildWeaponMap()
    {
        weaponMap = new Dictionary<GunType, GunScriptableObject>();
        for (int i = 0; i < weaponsByType.Length; i++)
        {
            if (weaponsByType[i] != null)
            {
                GunType type = (GunType)i;
                weaponMap[type] = weaponsByType[i];
            }
        }
    }

    public bool EquipWeapon(GunType weaponType)
    {
        if (!weaponMap.ContainsKey(weaponType))
        {
            Debug.LogWarning($"[EquipmentManager] Weapon type {weaponType} not configured.");
            return false;
        }

        var gun = weaponMap[weaponType];
        if (gunController.EquipGun(gun))
        {
            currentEquippedType = weaponType;
            return true;
        }

        return false;
    }
public void EnableWeaponPhysics()
{
    if (gunController == null) return;
    
    var gunModel = gunController.GetGunModel();
    if (gunModel == null) return;
    
    Rigidbody[] rbComps = gunModel.GetComponentsInChildren<Rigidbody>();
    foreach (var rb in rbComps)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Random.insideUnitSphere * 5f;
    }
    
    Debug.Log($"[EquipmentManager] Enabled physics for gun");
}

    public void UnequipWeapon()
    {
        gunController.UnequipGun();
        currentEquippedType = default;
    }

    public GunType CurrentEquippedType => currentEquippedType;
    public GunController GunController => gunController;
    public bool HasWeapon => gunController.CurrentGun != null;
    public bool IsReloading => gunController.IsReloading;
    public bool IsAiming => gunController.IsAiming;
}
