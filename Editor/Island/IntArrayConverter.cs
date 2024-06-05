using System;
using System.Collections.Generic;

public class IntArrayConverter
{
    public static string Encode(int[] data)
    {
        if (Array.Exists(data, element => element < 0))
        {
            throw new ArgumentException("負の値はエンコードできません。");
        }

        // 差分エンコーディング
        int[] diffEncoded = new int[data.Length];
        diffEncoded[0] = data[0];
        for (int i = 1; i < data.Length; i++)
        {
            int diff = data[i] - data[i - 1];
            if (diff < 0)
            {
                throw new ArgumentException("負の差分はエンコードできません。");
            }
            diffEncoded[i] = diff;
        }

        List<byte> compressed = new List<byte>();
        for (int i = 0; i < diffEncoded.Length; i++)
        {
            int value = diffEncoded[i];
            int runLength = 1;
            while (i + 1 < diffEncoded.Length && diffEncoded[i + 1] == value)
            {
                runLength++;
                i++;
            }

            compressed.Add((byte)value);
            compressed.Add((byte)runLength);
        }

        return Convert.ToBase64String(compressed.ToArray());
    }

    public static int[] Decode(string compressedData)
    {
        byte[] compressedBytes = Convert.FromBase64String(compressedData);
        if (compressedBytes.Length % 2 != 0)
        {
            throw new ArgumentException("圧縮データの形式が正しくありません。");
        }

        List<int> decompressed = new List<int>();
        for (int i = 0; i < compressedBytes.Length; i += 2)
        {
            int value = compressedBytes[i];
            int runLength = compressedBytes[i + 1];
            for (int j = 0; j < runLength; j++)
            {
                decompressed.Add(value);
            }
        }

        if (decompressed.Count == 0)
        {
            throw new ArgumentException("圧縮データの形式が正しくありません。");
        }

        int[] original = new int[decompressed.Count];
        original[0] = decompressed[0];
        for (int i = 1; i < decompressed.Count; i++)
        {
            original[i] = original[i - 1] + decompressed[i];
        }

        return original;
    }
}