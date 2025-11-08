using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;

public static class HexBlockKey
{
    public static string ToHexBlocks(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            string hex = bytes[i].ToString("X2");
            char randomChar = "0123456789ABCDEF"[UnityEngine.Random.Range(0, 16)];
            char lastChar = hex[1];
            sb.Append(hex[0]);
            sb.Append(randomChar);
            sb.Append(hex[1]);
            sb.Append(lastChar);
        }
        return sb.ToString();
    }

    public static byte[] FromHexBlocks(string hexBlocks)
    {
        int byteCount = hexBlocks.Length / 4;
        byte[] bytes = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            string block = hexBlocks.Substring(i * 4, 4);
            char firstChar = block[0];
            char secondChar = block[2];
            char lastChar = block[3];

            if (lastChar != secondChar)
                throw new Exception($"Hex block key mismatch at block {i}");

            string hex = $"{firstChar}{secondChar}";
            bytes[i] = Convert.ToByte(hex, 16);
        }

        return bytes;
    }
}
