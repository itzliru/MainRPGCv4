using UnityEngine;
using System;
using System.Collections;
using UnityEngine.AI;
using VaultSystems.Data;
[RequireComponent(typeof(WeaponIKController))]
public class NPCEquipmentManager : MonoBehaviour
{
    public WeaponData currentWeaponData;
    public Transform weaponHoldPoint;
    public bool hasWeapon = false;

     public GameObject CurrentWeaponInstance => currentWeaponInstance; //expose to combatcontroller

    private GameObject currentWeaponInstance;
    private WeaponIKController ikController;
    private Animator animator;

    private void Awake()
    {
        ikController = GetComponent<WeaponIKController>();
        animator = GetComponent<Animator>();
    }

  public void EquipWeapon(WeaponData weapon, Action onEquipped = null)
{
    if (hasWeapon) UnequipWeapon();

    currentWeaponData = weapon;

    // Instantiate the weapon prefab
    currentWeaponInstance = Instantiate(weapon.weaponPrefab, weaponHoldPoint);
    currentWeaponInstance.transform.localPosition = Vector3.zero;
    currentWeaponInstance.transform.localRotation = Quaternion.identity;
    hasWeapon = true;

  // Set ammo in NPCDataContainer update currentAmmo int perclone
    var npcData = GetComponent<NPCDataContainer>();
    if (npcData != null)
        npcData.EquipWeaponRuntime(weapon);

    // Find the hand targets dynamically by name
    Transform rightTarget = currentWeaponInstance.transform.Find(weapon.rightHandTargetName);
    Transform leftTarget = currentWeaponInstance.transform.Find(weapon.leftHandTargetName);

    if (rightTarget == null || leftTarget == null)
    {
        Debug.LogWarning($"[NPCEquipmentManager] Weapon '{weapon.weaponID}' prefab missing hand targets!");
    }

    // Set IK targets
    ikController.SetIKTargets(rightTarget, leftTarget);
    ikController.BindFromWeapon(currentWeaponData, currentWeaponInstance);
    // Bind to the prefab and data
    //ikController.BindFromWeaponPrefab(currentWeaponInstance);
    // After: ikController.BindFromWeapon(currentWeaponData, currentWeaponInstance);
  //  if (ikController.aimRig)
 //   {
   // // Bind the AimRig constraints to the NPC’s aim target (handled by combat controller)
   //     ikController.BindAimRigConstraints();

  //  }


    // Optional: trigger animation event
    StartCoroutine(DoAfter(0.4f, () =>
    {
        ikController.EnableIK(true, 0.25f);
        animator.SetBool("HasWeapon", true);
        onEquipped?.Invoke();
    }));
}


    public void UnequipWeapon(Action onUnequipped = null)
    {
        if (!hasWeapon) return;

        ikController.EnableIK(false, 0.25f);
        animator.SetBool("HasWeapon", false);
         ikController.UnbindAllIK();
        Destroy(currentWeaponInstance);
        currentWeaponData = null;
        hasWeapon = false;

        onUnequipped?.Invoke();
    }

    private IEnumerator DoAfter(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }


 // 🔹 For random test purposes
    public void EquipRandomWeapon()
    {
        var weapons = Resources.LoadAll<WeaponData>("Weapons");
        if (weapons.Length == 0)
        {
            Debug.LogWarning("[NPCEquipmentManager] No weapons found in Resources/Weapons.");
            return;
        }

        var weapon = weapons[UnityEngine.Random.Range(0, weapons.Length)];
        EquipWeapon(weapon);
    }
}
