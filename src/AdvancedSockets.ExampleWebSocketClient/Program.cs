using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AdvancedSockets.WebSockets;

namespace AdvancedSockets.ExampleWebSocketClient
{
    class Program
    {
        static WebSocketClient client;

        static void Main(string[] args)
        {
            Console.Clear();

            client = new WebSocketClient();
            client.OnConnect += Client_OnConnect;
            client.OnDisconnect += Client_OnDisconnect;
            client.OnMessage += Client_OnMessage;
            client.OnError += Client_OnError;
            client.Connect(new Uri("ws://localhost:8081"));
        }

        private static void Client_OnConnect()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Connected");

            client.Send(Encoding.ASCII.GetBytes("Hello World!"));
        }

        private static void Client_OnDisconnect()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Disconnected");
        }

        private static void Client_OnMessage(byte[] message)
        {
            Console.WriteLine(Encoding.ASCII.GetString(message));
        }

        private static void Client_OnError(string error, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(error);

            if (ex != null)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
