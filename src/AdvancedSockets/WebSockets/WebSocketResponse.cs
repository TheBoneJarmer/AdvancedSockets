using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;

namespace AdvancedSockets.WebSockets
{
    public class WebSocketResponse
    {
        public string Key { get; set; }

        public WebSocketResponse()
        {

        }

        public void Parse(byte[] data)
        {
            Parse(Encoding.ASCII.GetString(data));
        }
        public void Parse(string data)
        {
            // For validation
            string connectionHeader = "";
            string upgradeHeader = "";

            // Convert all carriage returns and newlines to just newlines and split the string on the newlines
            string[] lines = data.Replace("\r\n", "\n").Split('\n');

            // Iterate over all lines and parse them
            foreach (string line in lines)
            {
                if (line.StartsWith("Connection"))
                {
                    connectionHeader = line.Replace("Connection: ", "");
                }
                if (line.StartsWith("Upgrade"))
                {
                    upgradeHeader = line.Replace("Upgrade: ", "");
                }

                if (line.StartsWith("Sec-WebSocket-Accept"))
                {
                    Key = line.Replace("Sec-WebSocket-Accept: ", "");
                }
            }

            // Validate the input
            if (connectionHeader == "")
            {
                throw new WebSocketException("Header 'Connection' is missing or empty");
            }
            if (upgradeHeader == "")
            {
                throw new WebSocketException("Header 'Upgrade' is missing or empty");
            }
            if (connectionHeader != "Upgrade")
            {
                throw new WebSocketException("Invalid connection header. Expected: Upgrade");
            }
            if (upgradeHeader != "websocket")
            {
                throw new WebSocketException("Invalid upgrade header. Expected: websocket");
            }

            if (Key == "")
            {
                throw new WebSocketException("Header 'Sec-WebSocket-Accept' is missing or empty");
            }
        }
    }
}