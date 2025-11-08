using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using VaultSystems.Data;
using VaultSystems.Invoker;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace VaultSystems.Data
{
    // The load priority boss—organizing the chaos with flair!
    public class LoadPriorityContainer : AdvancedDataContainer
    {
        [Header("Load Settings")]           // Set the loading VIP list!
        [Range(1, 6)] public int loadOrder = 1; // Priority number, strut your stuff
        public List<AdvancedDataContainer> memberContainers = new List<AdvancedDataContainer>(); // The crew to load
        public bool isInitialized;          // Are we good to go?

        public event Action OnAllInitialized; // Party’s ready—cheers!

        public IEnumerator InitializeAllAsync()
        {
            yield return null; // Take a quick breather

            memberContainers = memberContainers
                .Where(c => c != null)
                .OrderBy(c => c.LoadPriority)
                .ToList(); // Sort the VIPs

            foreach (var container in memberContainers)
            {
                var routine = container.RuntimeAsyncSetup();
                if (routine != null)
                    yield return routine;
                else
                    yield return new WaitForEndOfFrame(); // fallback wait
            }

            isInitialized = true;           // We’re live!
            MarkDirty(DirtyReason.RuntimeChange); // Added extra null for VaultBreakpoint placeholder
            OnAllInitialized?.Invoke();     // Ring the bell
            Debug.Log("[LoadPriorityContainer] All member containers initialized."); // Success party!
        }

        protected void RuntimeSetup()
        {
            DiscoverMembers();              // Kick off the discovery dance
            StartCoroutine(AsyncRuntimeSetup()); // Launch async magic
        }

        private IEnumerator AsyncRuntimeSetup()
        {
            // Optional: Add async checks or delays if needed
            yield return null;              // Give it a beat to settle

            // Example: Validate containers post-discovery
            if (memberContainers.Any(c => c == null))
            {
                Debug.LogWarning("[LoadPriorityContainer] Found null containers, cleaning up!");
                memberContainers.RemoveAll(c => c == null);
            }

            // Future-proof: Trigger event or sync with GlobalWorldManager
            MarkDirty(DirtyReason.RuntimeSetup); 
            Debug.Log("[LoadPriorityContainer] Runtime setup complete with flair!");
        }

        private void DiscoverMembers()
        {
            memberContainers.Clear();       // Wipe the slate clean
            var containers = FindObjectsOfType<AdvancedDataContainer>().ToList(); // Scout the crew
            containers.Remove(this);        // Don’t include myself, cheeky!
            memberContainers.AddRange(containers); // Roll out the red carpet
        }
    }
}
