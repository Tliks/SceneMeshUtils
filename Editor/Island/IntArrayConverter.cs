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
        List<byte> compressed = new List<byte>();
        foreach (int number in data)
        {
            bool hasNext = true;
            int value = number;
            while (hasNext)
            {
                byte chunk = (byte)(value & 0x7F); // 7bit取り出す
                value >>= 7; // 次の7bit準備
                if (value > 0)
                {
                    chunk |= 0x80; // 次がある場合、先頭1bitをセット
                }
                else
                {
                    hasNext = false; // 次がない場合終了
                }
                compressed.Add(chunk);
            }
        }
        return Convert.ToBase64String(compressed.ToArray());
    }


    public static int[] Decode(string compressedData)
    {
        byte[] compressedBytes = Convert.FromBase64String(compressedData);
        List<int> decompressed = new List<int>();
        int value = 0;
        int shift = 0;

        foreach (byte chunk in compressedBytes)
        {
            value |= (chunk & 0x7F) << shift; // 7bit復元
            if ((chunk & 0x80) == 0)
            {
                // 次がない場合、完全な数値を追加
                decompressed.Add(value);
                value = 0; // 次の数値のためにリセット
                shift = 0; // シフトもリセット
            }
            else
            {
                // 次がある場合、シフトを増やす
                shift += 7;
            }
        }

        return decompressed.ToArray();
    }

    public static string Encodeg(int[] data)
    {
        // int配列をバイト配列に変換
        byte[] byteArray = data.SelectMany(BitConverter.GetBytes).ToArray();
        
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(byteArray, 0, byteArray.Length);
            }
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }

    public static int[] Decodeg(string encodedData)
    {
        byte[] compressedData = Convert.FromBase64String(encodedData);
        
        using (var compressedStream = new MemoryStream(compressedData))
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        using (var resultStream = new MemoryStream())
        {
            gzipStream.CopyTo(resultStream);
            byte[] decompressedData = resultStream.ToArray();
            
            // バイト配列をint配列へ変換
            int[] result = new int[decompressedData.Length / sizeof(int)];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = BitConverter.ToInt32(decompressedData, i * sizeof(int));
            }
            return result;
        }
    }
}