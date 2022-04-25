using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AdvancedSockets.Sockets
{
    public class SocketServer
    {
        private Thread thread1;
        private Thread thread2;
        private int port;

        public bool Running { get; private set; }

        public SocketServer(int listenPort)
        {
            port = listenPort;
        }

        public void Start()
        {
            Running = true;

            thread1 = new Thread(Thread1_Callback);
            thread1.Start();

            thread2 = new Thread(Thread2_Callback);
            thread2.Start();
        }
        public void Stop()
        {
            Running = false;
        }

        private void Thread1_Callback()
        {
            IPHostEntry ipHostEntry = Dns.GetHostEntry(Environment.MachineName);
            IPAddress ipAddress = ipHostEntry.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, port);

            Console.WriteLine("Listening on " + ipHostEntry.HostName + ":" + port);
            Console.WriteLine("Listening on " + ipAddress + ":" + port);

            Listen(ipEndpoint);
        }
        private void Thread2_Callback()
        {
            IPHostEntry ipHostEntry = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = ipHostEntry.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, port);

            // If the machine is not connected to the internet, both the machine name and localhost will resolve to 127.0.0.1
            // This is going to cause an error because you cannot have multiple sockets listen to the same ip address
            if (CheckLocalResolving())
            {
                return;
            }

            Console.WriteLine("Listening on localhost:" + port);
            Console.WriteLine("Listening on " + ipAddress + ":" + port);

            Listen(ipEndpoint);
        }

        private bool CheckLocalResolving()
        {
            IPHostEntry ipHostEntry1 = Dns.GetHostEntry(Environment.MachineName);
            IPHostEntry ipHostEntry2 = Dns.GetHostEntry("localhost");
            IPAddress ipAddress1 = ipHostEntry1.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            IPAddress ipAddress2 = ipHostEntry2.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);

            // If the machine's IP is a APIPA address it can't make connection and is considered local too
            if (ipAddress1.ToString().StartsWith("169.254"))
            {
                return true;
            }

            return ipAddress1.ToString().Equals(ipAddress2.ToString());
        }

        private void Listen(IPEndPoint ipEndPoint)
        {
            byte[] buffer = new byte[2048];
            byte[] result = new byte[0];
            int bytesReceived = buffer.Length;

            Socket listener = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ipEndPoint);
            listener.Listen(1000000);

            while (Running)
            {
                Socket handler = listener.Accept();
                
                while (bytesReceived == buffer.Length)
                {
                    bytesReceived = handler.Receive(buffer);
                    result = result.Push(buffer.Slice(0, bytesReceived));
                }
    
                OnMessage?.Invoke(handler, result);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

                result = new byte[0];
                bytesReceived = buffer.Length;
            }

            listener.Shutdown(SocketShutdown.Both);
            listener.Close();
        }

        /* EVENTS */
        public delegate void OnMessageHandler(Socket client, byte[] message);
        public event OnMessageHandler OnMessage;
    }
}
