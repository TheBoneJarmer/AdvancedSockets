using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AdvancedSockets.WebSockets;

namespace AdvancedSockets.ExampleWebSocketServer
{
    class Program
    {
        static WebSocketServer server;

        static void Main(string[] args)
        {
            server = new WebSocketServer("ws://localhost:8081");
            server.OnClientConnect += Server_OnConnect;
            server.OnClientDisconnect += Server_OnDisconnect;
            server.OnClientMessage += Server_OnMessage;
            server.OnServerError += Server_OnServerError;
            server.OnClientError += Server_OnClientError;

            server.Start();
        }

        private static void Server_OnConnect(Client client)
        {
            Console.WriteLine($"Client {client.Id} connected");
        }

        private static void Server_OnDisconnect(Client client)
        {
            Console.WriteLine($"Client {client.Id} left");
        }

        private static void Server_OnMessage(Client client, byte[] message)
        {
            var msg = $"[{client.Id}] {Encoding.ASCII.GetString(message)}";

            server.Broadcast(msg);
            Console.WriteLine(msg);
        }

        private static void Server_OnServerError(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine(exception.StackTrace);
        }

        private static void Server_OnClientError(Client client, Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine(exception.StackTrace);
        }
    }
}
