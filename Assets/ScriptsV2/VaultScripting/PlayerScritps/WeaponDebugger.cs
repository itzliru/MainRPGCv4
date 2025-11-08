using UnityEngine;

public class WeaponDebugger : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private GunController gunController;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("[WeaponDebugger] === RIFLE EQUIP DEBUG ===");
            
            if (equipmentManager == null)
            {
                Debug.LogError("EquipmentManager is NULL!");
                return;
            }
            
            Debug.Log($"Has weapon before equip: {equipmentManager.HasWeapon}");
            
            bool equipSuccess = equipmentManager.EquipWeapon(GunType.Rifle);
            Debug.Log($"Equip success: {equipSuccess}");
            
            var currentGun = equipmentManager.GunController.CurrentGun;
            Debug.Log($"CurrentGun: {(currentGun != null ? currentGun.GunName : "NULL")}");
            
            var gunModel = gunController.GetGunModel();
            Debug.Log($"Gun model: {(gunModel != null ? gunModel.name : "NULL")}");
            
            if (gunModel != null)
            {
                Debug.Log($"  - Position: {gunModel.transform.position}");
                Debug.Log($"  - Parent: {gunModel.transform.parent?.name ?? "NONE"}");
                Debug.Log($"  - Active: {gunModel.activeSelf}");
                Debug.Log($"  - Layer: {LayerMask.LayerToName(gunModel.layer)}");
                
                var firePoint = gunModel.transform.Find("FirePoint");
                Debug.Log($"  - FirePoint found: {(firePoint != null)}");
                
                var leftGrip = gunModel.transform.Find("LeftGrip");
                Debug.Log($"  - LeftGrip found: {(leftGrip != null)}");
                
                var rightGrip = gunModel.transform.Find("RightGrip");
                Debug.Log($"  - RightGrip found: {(rightGrip != null)}");
            }
            
            var rifleSO = equipmentManager.GunController.CurrentGun;
            if (rifleSO != null)
            {
                Debug.Log($"Rifle SO:");
                Debug.Log($"  - ModelPrefab: {(rifleSO.ModelPrefab != null ? rifleSO.ModelPrefab.name : "NULL - THIS IS THE PROBLEM!")}");
                Debug.Log($"  - BulletPrefab: {(rifleSO.BulletPrefab != null ? rifleSO.BulletPrefab.name : "NULL")}");
                Debug.Log($"  - ShootConfig: {(rifleSO.ShootConfig != null ? "OK" : "NULL")}");
            }
        }
    }
}
