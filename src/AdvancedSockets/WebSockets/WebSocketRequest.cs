using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using AdvancedSockets.Http;

namespace AdvancedSockets.WebSockets
{
    public class WebSocketRequest
    {
        public string Key { get; set; }
        public string Path { get; set; }
        public string Host { get; set; }
        public string Origin { get; set; }
        public string Protocol { get; set; }

        public WebSocketRequest(Uri uri)
        {
            Path = uri.AbsolutePath;
            Protocol = "chat, superchat";
            Host = uri.Host;
            Origin = "localhost";
            Key = GenerateKey();
        }

        public string GenerateKey(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)new Random().Next(0, 255);
            }

            return Convert.ToBase64String(bytes);
        }
        public string GenerateKey()
        {
            return GenerateKey(new byte[16]);
        }

        public byte[] ToHttpRequest()
        {
            string data = "";

            data += "GET " + Path + "HTTP/1.1\r\n";
            data += "Host: " + Host + "\r\n";
            data += "Upgrade: websocket\r\n";
            data += "Connection: Upgrade\r\n";
            data += "Sec-WebSocket-Key: " + Key + "\r\n";
            data += "Origin: " + Origin + "\r\n";
            data += "Sec-WebSocket-Protocol: " + Protocol + "\r\n";
            data += "Sec-WebSocket-Version: 13\r\n";
            data += "\r\n";

            return Encoding.ASCII.GetBytes(data);
        }
    }
}