using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AdvancedSockets.Sockets
{
    public class SocketClient
    {
        private IPHostEntry ipHostEntry;
        private IPAddress ipAddress;
        private IPEndPoint ipEndPoint;
        private Socket socket;

        public SocketClient()
        {
            
        }

        public void Connect(string host, int port)
        {
            ipHostEntry = Dns.GetHostEntry(host);
            ipAddress = ipHostEntry.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            ipEndPoint = new IPEndPoint(ipAddress, port);
            socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public byte[] Send(byte[] data)
        {
            byte[] buffer = new byte[2048];
            byte[] result = new byte[0];
            int bytesReceived = buffer.Length;

            socket.Connect(ipEndPoint);            
            socket.Send(data);

            while (bytesReceived == buffer.Length)
            {
                bytesReceived = socket.Receive(buffer);
                result = result.Push(buffer.Slice(0, bytesReceived));
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            return result;
        }
    }
}
