using UnityEngine;
using VaultSystems.Invoker;
using VaultSystems.Weapons;

public class PlayerWeaponInput : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private bool validateLayers = true;

    private GunController gunController;

    private void Start()
    {
        if (equipmentManager != null)
            gunController = equipmentManager.GunController;
    }

    private void Update()
    {
        if (!ValidateWeapon() || !ValidateCaseLayer())
            return;

        var caseCtrl = PlayerCaseController.Instance;
        if (caseCtrl != null)
        {
            if (caseCtrl.IsWeaponLocked())
            {
                gunController?.SetState(GunController.GunState.Locked);
                return;
            }
            else if (gunController != null && gunController.CurrentState == GunController.GunState.Locked)
            {
                gunController.UnlockFromCases(); // back to standby
            }
        }

        HandleAiming();
        HandleFiring();
        HandleReload();
        HandleWeaponSwitching();
    }

    private bool ValidateWeapon()
    {
        if (equipmentManager == null)
        {
            Debug.LogError("[PlayerWeaponInput] EquipmentManager not assigned.");
            return false;
        }
        return equipmentManager.HasWeapon;
    }

    private bool ValidateCaseLayer()
    {
        if (!validateLayers) return true;
        var caseCtrl = PlayerCaseController.Instance;
        if (caseCtrl == null) return true;

        var currentCase = caseCtrl.GetCurrentCase();
        // Allow weapon input if case is NOT blocking
        return !PlayerCaseController.IsBlockingCase(currentCase);
    }

    private void HandleAiming()
    {
        bool aimInput = Input.GetMouseButton(1);
        var caseCtrl = PlayerCaseController.Instance;
        
        if (caseCtrl == null || !equipmentManager.HasWeapon)
            return;

        if (aimInput && !caseCtrl.HasCase(PlayerCaseController.PlayerCase.Aim))
            caseCtrl.PushCase(PlayerCaseController.PlayerCase.Aim);
        else if (!aimInput && caseCtrl.HasCase(PlayerCaseController.PlayerCase.Aim))
            caseCtrl.PopCase(PlayerCaseController.PlayerCase.Aim);
    }

    private void HandleFiring()
    {
        bool fireInput = Input.GetMouseButton(0);
        var caseCtrl = PlayerCaseController.Instance;

        if (fireInput && equipmentManager.HasWeapon)
        {
            if (caseCtrl != null && !caseCtrl.HasCase(PlayerCaseController.PlayerCase.Aim))
                caseCtrl.PushCase(PlayerCaseController.PlayerCase.Aim);

            if (caseCtrl != null && caseCtrl.HasCase(PlayerCaseController.PlayerCase.Aim))
            {
                var firePointTransform = gunController.GetFirePoint();
                Vector3 firePosition = firePointTransform != null ? firePointTransform.position : transform.position;
                Vector3 fireDirection = GetFireDirection();
                gunController.TryFire(firePosition, fireDirection);
                ActivateLaser();
            }
        }
        else
        {
            DeactivateLaser();
        }
    }

    private void ActivateLaser()
    {
        if (gunController == null) return;
        var gunModel = gunController.GetGunModel();
        if (gunModel == null) return;
        var laserGizmo = gunModel.GetComponent<LaserAimGizmo>();
        laserGizmo?.SetLaserActive(true);
    }

    private void DeactivateLaser()
    {
        if (gunController == null) return;
        var gunModel = gunController.GetGunModel();
        if (gunModel == null) return;
        var laserGizmo = gunModel.GetComponent<LaserAimGizmo>();
        laserGizmo?.SetLaserActive(false);
    }

    private Vector3 GetFireDirection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return ray.direction;
    }

    private void HandleReload()
    {
        if (Input.GetKeyDown(KeyCode.R))
            gunController.TryReload();
    }



/// <summary>
/// this will be changed later to read from a different method like should i have a weapon right now?
/// </summary>
    private int currentIndex = -1;
    private void HandleScrollSwitch()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;

        int totalWeapons = 8;

        if (scroll > 0f)
            currentIndex = (currentIndex + 1) % totalWeapons;
        else
            currentIndex = (currentIndex - 1 + totalWeapons) % totalWeapons;

        GunType nextWeapon = (GunType)currentIndex;
        equipmentManager.EquipWeapon(nextWeapon);
    }

    private void HandleWeaponSwitching()
    {
        GunType? selectedType = null;

        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedType = GunType.Pistol;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) selectedType = GunType.Rifle;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) selectedType = GunType.Shotgun;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) selectedType = GunType.Sniper;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) selectedType = GunType.SMG;
        else if (Input.GetKeyDown(KeyCode.Alpha6)) selectedType = GunType.LMG;
        else if (Input.GetKeyDown(KeyCode.Alpha7)) selectedType = GunType.GrenadeLauncher;
        else if (Input.GetKeyDown(KeyCode.Alpha8)) selectedType = GunType.RocketLauncher;
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (equipmentManager.HasWeapon)
                equipmentManager.UnequipWeapon();
            return;
        }

        if (selectedType == null)
            return;

        if (equipmentManager.HasWeapon && equipmentManager.CurrentEquippedType == selectedType.Value)
        {
            equipmentManager.UnequipWeapon();
            Debug.Log($"[PlayerWeaponInput] Unequipped {selectedType.Value}");
        }
        else
        {
            if (equipmentManager.EquipWeapon(selectedType.Value))
                Debug.Log($"[PlayerWeaponInput] Equipped {selectedType.Value}");
            else
                Debug.LogWarning($"[PlayerWeaponInput] Could not equip {selectedType.Value}");
        }
    }
}
