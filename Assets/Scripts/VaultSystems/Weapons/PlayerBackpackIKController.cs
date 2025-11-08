using UnityEngine;

public class PlayerBackpackIKController : MonoBehaviour
{
    [Header("IK Settings")]
    public float ikBlendSpeed = 5f;
    public float backpackIKWeight = 0f; 
    private float targetBackpackIKWeight = 0f;

    [Header("Backpack IK")]
    private Transform backpackIKTarget;
    private Transform backpackSocketWorld;
    private Vector3 backpackTargetPos;
    private Quaternion backpackTargetRot;
    private GameObject backpackModel;
    private Animator animator;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError("[BackpackIK] No Animator found!");
    }

    private void Update()
    {
        UpdateBackpackIKBlending();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isActiveAndEnabled) return;
        if (layerIndex != 1) return;
        if (backpackIKWeight < 0.01f) return;

        // Backpack IK
        if (backpackIKTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, backpackIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, backpackIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, backpackIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, backpackIKTarget.rotation);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, backpackIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, backpackIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, backpackIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, backpackIKTarget.rotation);
        }
    }

    /// <summary>Initialize backpack IK targets</summary>
    public void InitializeBackpackIK(Transform worldSocket)
    {
        if (worldSocket == null) return;
        backpackSocketWorld = worldSocket;

        if (backpackIKTarget == null)
        {
            var go = new GameObject("BackpackIKTarget");
            go.transform.SetParent(transform);
            backpackIKTarget = go.transform;
        }

        backpackTargetPos = backpackIKTarget.position;
        backpackTargetRot = backpackIKTarget.rotation;
    }

    /// <summary>Set backpack IK active/inactive</summary>
    public void SetBackpackIKActive(bool active)
    {
        targetBackpackIKWeight = active ? 1f : 0f;
    }

    /// <summary>Update backpack IK blending</summary>
    private void UpdateBackpackIKBlending()
    {
        if (animator == null || animator.layerCount < 2) return;
        backpackIKWeight = Mathf.Lerp(backpackIKWeight, targetBackpackIKWeight, Time.deltaTime * ikBlendSpeed);
    }

    /// <summary>Lerp backpack IK to world socket during pickup</summary>
    public void UpdateBackpackIKToSocket()
    {
        if (backpackIKTarget == null || backpackSocketWorld == null) return;
        
        backpackModel = transform.Find("Inventory_Sack")?.gameObject;
        if (backpackModel != null)
            backpackModel.SetActive(true);

        backpackTargetPos = Vector3.Lerp(backpackTargetPos, backpackSocketWorld.position, 0.1f);
        backpackTargetRot = Quaternion.Lerp(backpackTargetRot, backpackSocketWorld.rotation, 0.1f);
        backpackIKTarget.SetPositionAndRotation(backpackTargetPos, backpackTargetRot);
    }

    /// <summary>Get backpack IK target for external systems</summary>
    public Transform GetBackpackIKTarget() => backpackIKTarget;

    public void SetBackpackVisible(bool visible)
    {
        if (backpackModel == null)
            backpackModel = transform.Find("Inventory_Sack")?.gameObject;

        var packRoot = transform.Find("Lira_Player")?.gameObject;

        if (backpackModel != null && packRoot != null)
        {
            backpackModel.transform.SetParent(packRoot.transform);
            backpackModel.transform.localPosition = Vector3.zero;
            backpackModel.transform.localRotation = Quaternion.identity;
            backpackModel.SetActive(visible);
            Debug.Log($"[Backpack] {(visible ? "Shown" : "Hidden")}");
        }
    }
}
