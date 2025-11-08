using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.CompilerServices;
using VaultSystems.Data;
using VaultSystems.Invoker;
using VaultSystems.Controllers;
namespace VaultSystems.Components
{
    public static class ComponentExtensions
    {
  
       /// <summary>
/// Shorthand version that returns the component or null
/// </summary>
public static T TryGetComponent<T>(this Component comp, string name) where T : Component
{
    if (comp == null) return null;
    return comp.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name)
           ?? (comp.GetComponentInParent<T>(true)?.name == name ? comp.GetComponentInParent<T>(true) : null)
           ?? comp.GetComponent<T>()
           ?? comp.GetComponentInParent<T>()
           ?? comp.GetComponentInChildren<T>();
}
/// <summary>
/// TryGetComponent with out parameter for compatibility
/// </summary>
public static bool TryGetComponent<T>(this Component comp, out T result, string name) where T : Component
{
    result = null;
    if (comp == null) return false; // ← missing semicolon added
    result = comp.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name)
             ?? (comp.GetComponentInParent<T>(true)?.name == name ? comp.GetComponentInParent<T>(true) : null)
             ?? comp.GetComponent<T>()
             ?? comp.GetComponentInParent<T>()
             ?? comp.GetComponentInChildren<T>();
    return result != null;
}
        /// <summary>
        /// Try to get component in children only
        /// </summary>
        public static bool TryGetComponentInChildren<T>(this Component comp, out T result, string name) where T : Component
        {
            result = comp.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
            return result != null;
        }
        /// <summary>
        /// Shorthand for children only
        /// </summary>
        public static T TryGetComponentInChildren<T>(this Component comp, string name) where T : Component
        {
            return comp.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
        }
        /// <summary>
        /// Try to get component in parent hierarchy
        /// </summary>
        public static bool TryGetComponentInParent<T>(this Component comp, out T result, string name) where T : Component
        {
            result = null;
            if (comp == null || comp.transform == null) return false;
            result = comp.GetComponentInParent<T>(true);
            if (result != null && result.name == name) return true;
            // Search up the hierarchy
            Transform parent = comp.transform.parent;
            while (parent != null)
            {
                result = parent.GetComponent<T>();
                if (result != null && result.name == name) return true;
                parent = parent.parent;
            }
            result = null;
            return false;
        }
        /// <summary>
        /// Shorthand for parent hierarchy
        /// </summary>
        public static T TryGetComponentInParent<T>(this Component comp, string name) where T : Component
        {
            T result = comp.GetComponentInParent<T>(true);
            if (result != null && result.name == name) return result;
            // Search up the hierarchy
            Transform parent = comp.transform.parent;
            while (parent != null)
            {
                result = parent.GetComponent<T>();
                if (result != null && result.name == name) return result;
                parent = parent.parent;
            }
            return null;
        }
        /// <summary>
        /// Get all components matching name in hierarchy
        /// </summary>
        public static IEnumerable<T> TryGetComponents<T>(this Component comp, string name) where T : Component
        {
            return comp.GetComponentsInChildren<T>(true).Where(c => c.name == name)
                   .Concat(comp.GetComponentsInParent<T>(true).Where(c => c.name == name))
                   .Distinct();
        }
        /// <summary>
        /// Check if component exists without getting it
        /// </summary>
        public static bool HasComponent<T>(this Component comp, string name = null) where T : Component
        {
            if (string.IsNullOrEmpty(name))
            {
                return comp.GetComponent<T>() != null ||
                       comp.GetComponentInParent<T>() != null ||
                       comp.GetComponentInChildren<T>() != null;
            }
            else
            {
                return comp.TryGetComponent<T>(name) != null;
            }
        }
        /// <summary>
        /// Ultra-short static method for component finding
        /// Usage: Component.Find<MyComponent>(gameObject, "name")
        /// </summary>
        public static T Find<T>(GameObject obj, string name = null) where T : Component
        {
            if (string.IsNullOrEmpty(name)) return obj.GetComponent<T>();
            return obj.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name)
                   ?? obj.GetComponentInParent<T>(true);
        }
        /// <summary>
        /// Even shorter static method - direct component access
        /// Usage: Component.Get<MyComponent>(transform, "name")
        /// </summary>
        public static T Get<T>(Component comp, string name = null) where T : Component
        {
            return string.IsNullOrEmpty(name) ? comp.GetComponent<T>() : comp.TryGetComponent<T>(name);
        }
        /// <summary>
        /// TryGetComponent with out parameter (Unity-style)
        /// Usage: if (transform.TryGet(out Rigidbody rb, "PhysicsBody")) { ... }
        /// </summary>
        public static bool TryGet<T>(this Component comp, out T result, string name = null) where T : Component
        {
            result = null;
            if (comp == null) return false;
            if (string.IsNullOrEmpty(name))
            {
                result = comp.GetComponent<T>() ?? comp.GetComponentInParent<T>() ?? comp.GetComponentInChildren<T>();
            }
            else
            {
                result = comp.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name)
                         ?? (comp.GetComponentInParent<T>(true)?.name == name ? comp.GetComponentInParent<T>(true) : null)
                         ?? comp.GetComponent<T>()
                         ?? comp.GetComponentInParent<T>()
                         ?? comp.GetComponentInChildren<T>();
            }
            return result != null;
        }
        /// <summary>
        /// Ultra-short method for component access with caching
       
        /// Usage: Component.G<MyComponent>(transform, "name")
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T G<T>(Component comp, string name = null) where T : Component =>
            comp != null
                ? (string.IsNullOrEmpty(name)
                    ? comp.GetComponent<T>() ?? comp.GetComponentInParent<T>() ?? comp.GetComponentInChildren<T>()
                    : comp.TryGetComponent<T>(name))
                : null;
        /// <summary>
        /// Find deep child - searches recursively through all descendants
        /// Usage: Component.FindDeep<MyComponent>(transform, "DeepChildName")
        /// </summary>
        public static T FindDeep<T>(Component comp, string name) where T : Component
        {
            if (comp == null || string.IsNullOrEmpty(name)) return null;
            return comp.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);
        }
        /// <summary>
        /// Find component by path (like "Parent/Child/GrandChild")
        /// Usage: Component.FindByPath<MyComponent>(transform, "UI/Canvas/Button")
        /// </summary>
        public static T FindByPath<T>(Component comp, string path) where T : Component
        {
            if (comp == null || string.IsNullOrEmpty(path) || comp.transform == null) return null;
            Transform target = comp.transform.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }
        public static T FindByPathDeep<T>(Component comp, string path) where T : Component
{
    if (comp == null || string.IsNullOrEmpty(path)) return null;
    Transform target = comp.transform.Find(path);
    if (target != null) return target.GetComponent<T>();
    // fallback: recursive search by name at any depth
    return comp.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == path);
}
        /// <summary>
        /// Get or add component if it doesn't exist
        /// Usage: Component.GetOrAdd<MyComponent>(gameObject)
        /// </summary>
        public static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            if (obj == null) return null;
            T comp = obj.GetComponent<T>();
            return comp != null ? comp : obj.AddComponent<T>();
        }
        /// <summary>
        /// Find component by tag in hierarchy
        /// Usage: Component.FindByTag<MyComponent>(transform, "Enemy")
        /// </summary>
        public static T FindByTag<T>(Component comp, string tag) where T : Component
        {
            if (comp == null || string.IsNullOrEmpty(tag)) return null;
            return comp.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.CompareTag(tag))
                   ?? comp.GetComponentInParent<T>(true);
        }
        /// <summary>
        /// Get all components of type in hierarchy (cached version)
        /// Usage: Component.GetAllInHierarchy<MyComponent>(transform)
        /// </summary>
        public static T[] GetAllInHierarchy<T>(Component comp) where T : Component
        {
            if (comp == null) return new T[0];
            return comp.GetComponentsInChildren<T>(true);
        }
        /// <summary>
        /// Safe component destruction with null check
        /// Usage: Component.SafeDestroy(myComponent)
        /// </summary>
        public static void SafeDestroy<T>(T component) where T : Component
        {
            if (component != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(component);
                else
                    Object.DestroyImmediate(component);
            }
        }
        /// <summary>
        /// Enable/disable multiple components at once
        /// Usage: Component.SetEnabled(transform, false, typeof(Renderer), typeof(Collider))
        /// </summary>
        public static void SetEnabled(Component comp, bool enabled, params System.Type[] componentTypes)
        {
            if (comp == null || componentTypes == null) return;
            foreach (var type in componentTypes)
            {
                var component = comp.GetComponent(type);
                if (component is Behaviour behaviour)
                    behaviour.enabled = enabled;
                else if (component is Renderer renderer)
                    renderer.enabled = enabled;
                else if (component is Collider collider)
                    collider.enabled = enabled;
            }
        }
        /// <summary>
        /// Copy component values from one to another
        /// Usage: Component.CopyValues(sourceComponent, targetComponent)
        /// </summary>
        public static void CopyValues<T>(T source, T target) where T : Component
        {
            if (source == null || target == null) return;
            try
            {
                var sourceType = source.GetType();
                var fields = sourceType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    try
                    {
                        field.SetValue(target, field.GetValue(source));
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Failed to copy field {field.Name}: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to copy component values: {ex.Message}");
            }
        }
        /// <summary>
        /// Struct to hold invoked components for easy access
        /// </summary>
        public struct InvokedComponents
        {
            public PlayerAnimator1 PlayerAnimator;
            public PlayerController PlayerController;
            public GunController GunController;
            public PlayerDataContainer PlayerDataContainer;
            // Dictionary for additional components
            private Dictionary<string, Component> additionalComponents;
            public InvokedComponents(PlayerAnimator1 playerAnim, PlayerController playerCtrl, GunController gunCtrl, PlayerDataContainer playerData)
            {
                PlayerAnimator = playerAnim;
                PlayerController = playerCtrl;
                GunController = gunCtrl;
                PlayerDataContainer = playerData;
                additionalComponents = new Dictionary<string, Component>();
            }
            /// <summary>
            /// Add additional component
            /// </summary>
            public void AddComponent(string key, Component component)
            {
                if (additionalComponents == null)
                    additionalComponents = new Dictionary<string, Component>();
                additionalComponents[key] = component;
            }
            /// <summary>
            /// Get additional component by key
            /// </summary>
            public T GetComponent<T>(string key) where T : Component
            {
                if (additionalComponents != null && additionalComponents.TryGetValue(key, out var comp))
                    return comp as T;
                return null;
            }
            /// <summary>
            /// Check if all main components are present
            /// </summary>
            public bool IsComplete => PlayerAnimator != null && PlayerController != null && GunController != null && PlayerDataContainer != null;
        }
        /// <summary>
        /// Invoke and cache multiple components at once
        /// Usage: var components = Component.InvokeComponents(transform);
        /// </summary>
        public static InvokedComponents InvokeComponents(Component root)
        {
            if (root == null || root.transform == null) return new InvokedComponents(null, null, null, null);
            // Find main components using the extension methods
            var playerAnim = root.TryGetComponent<PlayerAnimator1>("PlayerAnimator") ?? root.GetComponentInChildren<PlayerAnimator1>();
            var playerCtrl = root.TryGetComponent<PlayerController>("PlayerController") ?? root.GetComponentInChildren<PlayerController>();
            var gunCtrl = root.TryGetComponent<GunController>("GunController") ?? root.GetComponentInChildren<GunController>();
            var playerData = root.TryGetComponent<PlayerDataContainer>("PlayerDataContainer") ?? root.GetComponentInChildren<PlayerDataContainer>();
            return new InvokedComponents(playerAnim, playerCtrl, gunCtrl, playerData);
        }
        /// <summary>
        /// Invoke components with custom names
        /// Usage: var components = Component.InvokeComponents(transform, "MyAnimator", "MyController", "MyGun", "MyData");
        /// </summary>
        public static InvokedComponents InvokeComponents(Component root, string animatorName, string controllerName, string gunName, string dataName = "PlayerDataContainer")
        {
            if (root == null) return new InvokedComponents(null, null, null, null);
            var playerAnim = root.TryGetComponent<PlayerAnimator1>(animatorName);
            var playerCtrl = root.TryGetComponent<PlayerController>(controllerName);
            var gunCtrl = root.TryGetComponent<GunController>(gunName);
            var playerData = root.TryGetComponent<PlayerDataContainer>(dataName);
            return new InvokedComponents(playerAnim, playerCtrl, gunCtrl, playerData);
        }
        /// <summary>
        /// Invoke components and add additional ones
        /// Usage: var components = Component.InvokeComponentsWithExtras(transform, new Dictionary<string, System.Type> { {"Health", typeof(HealthSystem)} });
        /// </summary>
        public static InvokedComponents InvokeComponentsWithExtras(Component root, Dictionary<string, System.Type> extraComponents = null)
        {
            var invoked = InvokeComponents(root);
            if (extraComponents != null)
            {
                foreach (var pair in extraComponents)
                {
                    // Non-generic search mirroring the behavior of TryGetComponent<T>(string name)
                    Component component = root.GetComponentsInChildren(pair.Value, true).FirstOrDefault(c => c.name == pair.Key)
                                       ?? (root.GetComponentInParent(pair.Value, true)?.name == pair.Key ? root.GetComponentInParent(pair.Value, true) : null)
                                       ?? root.GetComponent(pair.Value)
                                       ?? root.GetComponentInParent(pair.Value)
                                       ?? root.GetComponentInChildren(pair.Value);
                    invoked.AddComponent(pair.Key, component);
                }
            }
            return invoked;
        }
    }
}