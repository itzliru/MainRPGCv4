using UnityEngine;

namespace VaultSystems.Weapons
{
    public class PlayerWeaponIKController : MonoBehaviour
    {
        [Header("IK Bone References")]
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftHandHint;
        [SerializeField] private Transform rightHandHint;
        [SerializeField] private Transform spine;

        [Header("IK Rig Layer")]
        [SerializeField] private Animator animator;
        [SerializeField] private int rigLayerIndex = 1;

        [Header("IK Settings")]
        [SerializeField] private float ikPositionSmoothness = 0.1f;
        [SerializeField] private float ikRotationSmoothness = 0.1f;

        private EquipmentManager equipmentManager;
        private GunController currentGun;
        private bool isIKActive = false;

        private Vector3 leftHandTargetPos;
        private Vector3 rightHandTargetPos;
        private Quaternion leftHandTargetRot;
        private Quaternion rightHandTargetRot;

        public void Initialize(PlayerAnimator1 playerAnimator)
        {
            animator = playerAnimator.animator;
            equipmentManager = GetComponentInParent<EquipmentManager>();

            if (animator == null)
                Debug.LogError("[WeaponIKController] No Animator found!");
            if (equipmentManager == null)
                Debug.LogError("[WeaponIKController] No EquipmentManager found!");

            InitializeIKTargets();
            SetIKLayerWeight(0f);
        }

        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }
            if (equipmentManager == null)
            {
                equipmentManager = GetComponentInParent<EquipmentManager>();
            }

            if (animator == null)
                Debug.LogError("[WeaponIKController] No Animator found!");
            if (equipmentManager == null)
                Debug.LogError("[WeaponIKController] No EquipmentManager found!");

            InitializeIKTargets();
            SetIKLayerWeight(0f);
        }

        private void InitializeIKTargets()
        {
            if (leftHandTarget == null)
                leftHandTarget = new GameObject("LeftHandIKTarget").transform;
            if (rightHandTarget == null)
                rightHandTarget = new GameObject("RightHandIKTarget").transform;
            if (leftHandHint == null)
                leftHandHint = new GameObject("LeftHandHint").transform;
            if (rightHandHint == null)
                rightHandHint = new GameObject("RightHandHint").transform;

            leftHandTargetPos = leftHandTarget.position;
            rightHandTargetPos = rightHandTarget.position;
            leftHandTargetRot = leftHandTarget.rotation;
            rightHandTargetRot = rightHandTarget.rotation;
        }

        private void Update()
        {
            if (equipmentManager == null || animator == null) return;

            currentGun = equipmentManager.GunController;
            bool hasWeapon = currentGun != null && currentGun.CurrentGun != null;

            if (hasWeapon && !isIKActive)
                ActivateIK();
            else if (!hasWeapon && isIKActive)
                DeactivateIK();

            if (isIKActive && currentGun != null)
            {
                if (currentGun.CurrentState == GunController.GunState.Locked)
                {
                    SetIKLayerWeight(0f);
                    return;
                }
                UpdateIKTargets();
            }
        }

        private void ActivateIK()
        {
            isIKActive = true;
            SetIKLayerWeight(1f);
            Debug.Log("[WeaponIKController] IK Activated");
        }

        private void DeactivateIK()
        {
            isIKActive = false;
            SetIKLayerWeight(0f);
            Debug.Log("[WeaponIKController] IK Deactivated");
        }

        private void SetIKLayerWeight(float weight)
        {
            if (animator != null && rigLayerIndex < animator.layerCount)
                animator.SetLayerWeight(rigLayerIndex, weight);
        }

        private void UpdateIKTargets()
        {
            if (currentGun == null) return;

            var gunModel = currentGun.GetGunModel();
            if (gunModel == null) return;

            Transform firePoint = currentGun.GetFirePoint();
            if (firePoint == null) return;

            UpdateHandIK(gunModel);
            UpdateSpineRotation(firePoint);
        }

        private void UpdateHandIK(GameObject gunModel)
        {
            Transform leftGrip = gunModel.transform.Find("LeftGrip");
            Transform rightGrip = gunModel.transform.Find("RightGrip");

            if (rightGrip != null && rightHandTarget != null)
            {
                rightHandTargetPos = Vector3.Lerp(rightHandTargetPos, rightGrip.position, ikPositionSmoothness);
                rightHandTargetRot = Quaternion.Lerp(rightHandTargetRot, rightGrip.rotation, ikRotationSmoothness);
                rightHandTarget.SetPositionAndRotation(rightHandTargetPos, rightHandTargetRot);
            }

            if (leftGrip != null && leftHandTarget != null)
            {
                leftHandTargetPos = Vector3.Lerp(leftHandTargetPos, leftGrip.position, ikPositionSmoothness);
                leftHandTargetRot = Quaternion.Lerp(leftHandTargetRot, leftGrip.rotation, ikRotationSmoothness);
                leftHandTarget.SetPositionAndRotation(leftHandTargetPos, leftHandTargetRot);
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (layerIndex != rigLayerIndex || !isIKActive || animator == null) return;

            // Apply IK to hands
            if (rightHandTarget != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
            }

            if (leftHandTarget != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
            }
        }

        private void UpdateSpineRotation(Transform firePoint)
        {
            if (spine == null || firePoint == null) return;

            Vector3 dirToFirePoint = (firePoint.position - spine.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dirToFirePoint, Vector3.up);
            spine.rotation = Quaternion.Lerp(spine.rotation, targetRot, ikRotationSmoothness * 0.5f);
        }

        public bool IsIKActive => isIKActive;
    }
    
    
}
