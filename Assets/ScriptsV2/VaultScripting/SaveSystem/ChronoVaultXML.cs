using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;

namespace VaultSystems.Data
{
[System.Serializable]
public class XmlVault
{
    public string encryptedData;
}

public static class ChronoVaultXML
{
    private static string GetFilePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"quantumSave_{slot}.xml");
    }

    /// <summary>
    /// Save any serializable container to an encrypted Hex XML file
    /// </summary>
    public static void LockVault<T>(T container, int slot)
    {
        try
        {
            // Convert object to JSON
            string json = JsonUtility.ToJson(container);

            // Encrypt JSON bytes
            byte[] encryptedBytes = FluxCapacitor.WarpEncryptBytes(System.Text.Encoding.UTF8.GetBytes(json));

            // Convert to hex blocks
            string hexBlocks = HexBlockKey.ToHexBlocks(encryptedBytes);

            // Wrap in XmlVault
            XmlVault vault = new XmlVault { encryptedData = hexBlocks };

            // Serialize to XML file
            XmlSerializer serializer = new XmlSerializer(typeof(XmlVault));
            using (FileStream stream = new FileStream(GetFilePath(slot), FileMode.Create))
            {
                serializer.Serialize(stream, vault);
            }

            Debug.Log($"[ChronoVault] Saved slot {slot}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[ChronoVault] Failed to save slot " + slot + ": " + e.Message);
        }
    }

    /// <summary>
    /// Load a container from a Hex XML file
    /// </summary>
    public static T UnlockVault<T>(int slot)
    {
        string path = GetFilePath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[ChronoVault] Save slot {slot} not found.");
            return default;
        }

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlVault));
            XmlVault vault;

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                vault = (XmlVault)serializer.Deserialize(stream);
            }

            // Convert hex blocks back to bytes
            byte[] encryptedBytes = HexBlockKey.FromHexBlocks(vault.encryptedData);

            // Decrypt
            byte[] decryptedBytes = FluxCapacitor.WarpDecryptBytes(encryptedBytes);

            // JSON → object
            string json = System.Text.Encoding.UTF8.GetString(decryptedBytes);

            return JsonUtility.FromJson<T>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[ChronoVault] Failed to load slot " + slot + ": " + e.Message);
            return default;
        }
    }

    /// <summary>
    /// Check if a slot exists
    /// </summary>
    public static bool SlotExists(int slot)
    {
        return File.Exists(GetFilePath(slot));
    }
    public static string GetSaveFilePath(int slot)
{
    return Path.Combine(Application.persistentDataPath, $"quantumSave_{slot}.xml");
}

}
}