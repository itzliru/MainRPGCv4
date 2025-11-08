using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using VaultSystems.Data;
// Then reference it as DynamicObjectManager.Instance
namespace VaultSystems.Invoker
{
    public enum MethodLayer
    {
        Func,      // single-function lambdas
        Overlay,   // add-on behaviors
        Blocking   // disables / high priority
    }

    public class DynamicMethodSwitch : IDisposable 
    {
        public Action Method;
        public bool Enabled = true;
        public MethodLayer Layer = MethodLayer.Func;

        private bool _disposed = false;

        public DynamicMethodSwitch(Action method, MethodLayer layer = MethodLayer.Func)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            Layer = layer;
            VaultSystems.Invoker.Invoke.Register(this);
        }

        public void Tick()
        {
            if (_disposed || !Enabled) return;
            try
            {
                Method?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DynamicMethodSwitch] Exception in method: {ex}");
            }
        }

        public void Disable() => Enabled = false;
        public void Enable() => Enabled = true;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Enabled = false;
            Method = null;
            VaultSystems.Invoker.Invoke.Unregister(this);
        }
    }

    public partial class Invoke : MonoBehaviour
    {
        private static readonly List<DynamicMethodSwitch> dynamicMethods = new();

        public static DynamicMethodSwitch Wrap(Action method, MethodLayer layer = MethodLayer.Func)
        {
            return new DynamicMethodSwitch(method, layer);
        }

        internal static void Register(DynamicMethodSwitch dyn)
        {
            if (!dynamicMethods.Contains(dyn))
                dynamicMethods.Add(dyn);
        }

        internal static void Unregister(DynamicMethodSwitch dyn)
        {
            dynamicMethods.Remove(dyn);
        }

        private void Update()
        {
            if (dynamicMethods.Count == 0) return;

            // Snapshot copy to avoid modification during iteration
            var buffer = dynamicMethods.ToArray();
            foreach (var m in buffer)
                m.Tick();
        }

        /// <summary>
        /// Enable or disable all methods of a given layer
        /// </summary>
        public static void SetLayerEnabled(MethodLayer layer, bool enabled)
        {
            foreach (var m in dynamicMethods)
                if (m.Layer == layer)
                    m.Enabled = enabled;
        }
    }
}
