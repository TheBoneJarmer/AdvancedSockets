using System;
using System.Security.Cryptography;
using System.Text;

namespace AdvancedSockets.WebSockets
{
    public static class WebSocketUtils
    {
        public static string GenerateRequestKey()
        {
            return GenerateRequestKey(new byte[16]);
        }
        public static string GenerateRequestKey(byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)new Random().Next(0, 255);
            }

            return Convert.ToBase64String(bytes);
        }

        public static string GenerateResponseKey(string input)
        {
            var sha1 = SHA1.Create();

            // First, concatenate the input with a guid specified by RFC 6455
            string concatenation = input + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            // Second, convert the result of the concatenation to bytes
            byte[] concatenationBytes = Encoding.ASCII.GetBytes(concatenation);

            // Third, hash those bytes using the SHA1 algorithm
            byte[] concatenationBytesHashed = sha1.ComputeHash(concatenationBytes);

            // And finally, convert the result to a base64 string
            return Convert.ToBase64String(concatenationBytesHashed);
        }
    }
}