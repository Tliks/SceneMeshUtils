using System;
using System.Text;

public class IntArrayConverter
{
    public static string Encode(int[] input)
    {
        if (input == null || input.Length == 0)
        {
            return null;
        }

        StringBuilder sb = new StringBuilder();
        foreach (int i in input)
        {
            sb.Append(i).Append(",");
        }

        if (sb.Length > 0)
        {
            sb.Length--;
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public static int[] Decode(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        try
        {
            string decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(input));
            string[] split = decodedString.Split(',');

            int[] result = new int[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                result[i] = int.Parse(split[i]);
            }

            return result;
        }
        catch
        {
            return null;
        }
    }
}