using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using VaultSystems.Data;

namespace VaultSystems.Data
{
public static class HexSerializationHelper
{
    public static string ToHex(IDataContainer container)
    {
        if (container == null) return null;

        try
        {
            string json = JsonUtility.ToJson(container, false);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return BitConverter.ToString(bytes).Replace("-", "");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HexSerializationHelper] Serialize failed: {e}");
            return null;
        }
    }

    public static void FromHex(IDataContainer container, string hexData)
    {
        if (container == null || string.IsNullOrEmpty(hexData)) return;

        try
        {
            int len = hexData.Length / 2;
            byte[] bytes = new byte[len];
            for (int i = 0; i < len; i++)
                bytes[i] = Convert.ToByte(hexData.Substring(i * 2, 2), 16);

            string json = System.Text.Encoding.UTF8.GetString(bytes);
            JsonUtility.FromJsonOverwrite(json, container);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HexSerializationHelper] Deserialize failed: {e}");
        }
    }
}
}