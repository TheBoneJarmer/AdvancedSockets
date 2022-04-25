using System;
using System.Net.Sockets;
using System.Threading;

namespace AdvancedSockets.Sockets
{
    public class SocketAccept
    {
        public Socket Socket { get; set; }
        public ManualResetEvent ResetEvent { get; set; }
    }
}