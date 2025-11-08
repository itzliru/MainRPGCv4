using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Data;

namespace VaultSystems.Data
{
[DisallowMultipleComponent]
public class DataContainerUpdater : MonoBehaviour
{
    public bool autoUpdate = false;
    public float updateInterval = 5f;

    private float timer;
    private IDataContainer dataComp;

    private void Awake()
    {
        dataComp = GetComponent<IDataContainer>();

        // 🔗 Subscribe if the container supports change events
        if (dataComp != null)
        {
            dataComp.OnDataChanged += HandleDataChanged;
        }
    }

    private void OnDestroy()
    {
        if (dataComp != null)
            dataComp.OnDataChanged -= HandleDataChanged;
    }

    private void Update()
    {
        if (autoUpdate)
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                timer = 0f;
                UpdateContainer();
            }
        }
    }

    /// <summary>
    /// Call manually or automatically to save this data container.
    /// </summary>
    public void UpdateContainer()
    {
        if (dataComp == null) return;

        string hex = dataComp.SerializeData();
        Debug.Log($"[DataContainerUpdater] Serialized DataContainer for {gameObject.name}: {hex.Substring(0, Mathf.Min(20, hex.Length))}...");
    }

    private void HandleDataChanged()
    {
        // 🧠 Debounce: if called multiple times in same frame, delay a little
        CancelInvoke(nameof(UpdateContainer));
        Invoke(nameof(UpdateContainer), 0.1f);
    }
}
}