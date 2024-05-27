using System;
using System.Text;
using System.Collections.Generic;

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
}
