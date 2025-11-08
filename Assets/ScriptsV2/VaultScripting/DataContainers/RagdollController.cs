using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Controllers;

namespace VaultSystems.Controllers
{
    
    public class RagdollController : MonoBehaviour
    {
        private Rigidbody[] rigidbodies;
        private Collider[] colliders;
        private Animator animator;
        private bool isRagdollEnabled = false;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rigidbodies = GetComponents<Rigidbody>();
            colliders = GetComponents<Collider>();

            // Start with physics disabled
            DisablePhysics();
        }

        public void EnablePhysics()
        {
            if (isRagdollEnabled) return;

            animator.enabled = false;  // Disable animation

            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            foreach (var col in colliders)
            {
                col.enabled = true;
            }

            isRagdollEnabled = true;
            Debug.Log($"[RagdollController] Physics enabled on {gameObject.name}");
        }

        public void DisablePhysics()
        {
            if (!isRagdollEnabled) return;

            animator.enabled = true;

            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            isRagdollEnabled = false;
        }

        public bool IsRagdollActive => isRagdollEnabled;
    }
}
