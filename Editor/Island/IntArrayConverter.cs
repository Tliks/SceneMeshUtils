using System;
using System.Text;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

public class IntArrayConverter
{
    public static string Encode(int[] data)
    {
        // 差分エンコーディング
        int[] diffEncoded = new int[data.Length];
        diffEncoded[0] = data[0];
        for (int i = 1; i < data.Length; i++)
        {
            diffEncoded[i] = data[i] - data[i - 1];
        }

        // ランレングス圧縮
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

            compressed.Add((byte)value); // 差分値を格納
            compressed.Add((byte)runLength); // ランレングスを格納
        }

        return Convert.ToBase64String(compressed.ToArray());
    }

    public static int[] Decode(string compressedData)
    {
        byte[] compressedBytes = Convert.FromBase64String(compressedData);
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

        // 差分復元
        int[] original = new int[decompressed.Count];
        original[0] = decompressed[0];
        for (int i = 1; i < decompressed.Count; i++)
        {
            original[i] = original[i - 1] + decompressed[i];
        }

        return original;
    }
}