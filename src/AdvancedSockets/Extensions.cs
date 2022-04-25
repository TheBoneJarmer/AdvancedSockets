using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AdvancedSockets
{
    public static class Extensions
    {
        public static string ByteToBinaryString(this byte b)
        {
            return Convert.ToString(b, 2).PadLeft(8, '0');
        }
        public static string IntToBinaryString(this int i)
        {
            return Convert.ToString(i, 2).PadLeft(8, '0');
        }
        public static char[] ByteToBinaryCharArray(this byte b)
        {
            return ByteToBinaryString(b).ToCharArray();
        }
        public static byte[] ByteToBinaryByteArray(this byte b)
        {
            return ByteToBinaryCharArray(b).Select(x => Convert.ToByte(x.ToString())).ToArray();
        }
        public static int BinaryStringToInt(this string s)
        {
            return Convert.ToInt32(s, 2);
        }
        public static byte BinaryStringToByte(this string s)
        {
            return Convert.ToByte(s, 2);
        }
        public static byte BinaryByteArrayToByte(this byte[] b)
        {
            return string.Join("", b).BinaryStringToByte();
        }
        public static int BinaryByteArrayToInt(this byte[] b)
        {
            return string.Join("", b).BinaryStringToInt();
        }

        public static T[] Push<T>(this T[] source, T[] dest)
        {
            var result = new List<T>();
            result.AddRange(source);
            result.AddRange(dest);

            return result.ToArray();
        }
        public static T[] Slice<T>(this T[] data, int index)
        {
            return data.Slice<T>(index, data.Length - index);
        }
        public static T[] Slice<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);

            return result;
        }
    }
}
