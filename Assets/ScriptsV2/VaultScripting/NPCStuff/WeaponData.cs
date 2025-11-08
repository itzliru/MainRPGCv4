using UnityEngine;

[CreateAssetMenu(menuName = "Vault/Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponID;
    public GameObject weaponPrefab;
    [Header("Visuals & Audio (by name)")]
    public string muzzleFlashPrefabName = "MuzzleFlash"; // under Resources/MuzzleFlashes/
    public string fireSFXName = "GunFire_Default";                    // under Resources/Sounds/
    public float fireVolume = 0.9f;

   [Header("Weapon")]
    public string bulletPrefabName = "_bulletPrefab"; 
 
    public string firePointName = "firePoint";
    public bool isTwoHanded;
    public float fireRate = 4f;
    public float bulletSpeed = 20f;
    public int ammo = 50;

    // Instead of Transform references, store names
    public string rightHandTargetName = "rightHandTarget";
    public string leftHandTargetName = "leftHandTarget";

    public string equipAnimation = "Equip";
    public string idleAnimation = "IdleWeapon";
    public string attackAnimation = "Attack";

    [Range(0, 1f)] public float weaponLayerWeight = 1f;
    public int weaponLayerIndex = 1;



    
}
