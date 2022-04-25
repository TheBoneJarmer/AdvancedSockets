using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace AdvancedSockets.Sockets
{
    public class SocketMessage
    {
        public Socket Socket { get; set; }
        public byte[] Buffer { get; set; }
        public byte[] Data { get; set; }

        public SocketMessage()
        {
            Buffer = new byte[1024 * 1024];
            Data = new byte[0];
        }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(Data);
        }
    }
}