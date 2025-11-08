using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public static class FluxCapacitor
{
    private static byte[] quantumKey = Encoding.UTF8.GetBytes("Mysterio42");

    public static byte[] WarpEncryptBytes(byte[] input)
    {
        byte[] xor = Entangle(input);
        for (int i = 0; i < xor.Length; i++)
            xor[i] = (byte)(xor[i] + ((i % 7) + 1));
        return xor;
    }

    public static byte[] WarpDecryptBytes(byte[] input)
    {
        byte[] shifted = new byte[input.Length];
        for (int i = 0; i < input.Length; i++)
            shifted[i] = (byte)(input[i] - ((i % 7) + 1));
        return Entangle(shifted);
    }

    private static byte[] Entangle(byte[] bytes)
    {
        byte[] output = new byte[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
            output[i] = (byte)(bytes[i] ^ quantumKey[i % quantumKey.Length]);
        return output;
    }
}
