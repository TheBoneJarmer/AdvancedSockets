using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AdvancedSockets.Http;
using AdvancedSockets.Http.Server;

namespace AdvancedSockets.WebSockets
{
    public class WebSocketServer
    {
        private Thread threadUpdate;
        private Thread threadListen;
        
        public Uri Uri { get; private set; }
        public int ReceiveTimeout { get; set; }
        public int SendTimeout { get; set;  }
        public int MaxConnections { get; set; }
        public int BufferSize { get; set;  }
        public int StackSize { get; set; }
        public List<Client> Clients { get; private set; }
        public bool Running { get; private set; }

        public WebSocketServer(string url) : this(new Uri(url))
        {

        }
        public WebSocketServer(Uri uri)
        {
            Clients = new List<Client>();

            this.ReceiveTimeout = 1000 * 60 * 30;
            this.SendTimeout = 1000 * 30;
            this.MaxConnections = 1000;
            this.BufferSize = 1024 * 1024 * 4; // 4MB
            this.Uri = uri;
            this.StackSize = 1024 * 1024; // 1MB
        }

        public void Start()
        {
            Running = true;

            // Start the update thread
            threadUpdate = new Thread(UpdateThreadCallback);
            threadUpdate.Start();

            // Start the listen threads
            threadListen = new Thread(ListenThreadCallback);
            threadListen.Start();
        }

        public void Stop()
        {
            Running = false;

            // Stop each client
            foreach (var client in Clients)
            {
                client.AbortConnection();
            }
        }

        public void Broadcast(string data)
        {
            Broadcast(Encoding.ASCII.GetBytes(data));
        }
        public void Broadcast(byte[] data)
        {
            foreach (Client client in Clients)
            {
                client.Send(data);
            }
        }
        /* THREAD CALLBACKS */
        private void ListenThreadCallback()
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(Uri.Host);

                if (hostEntry.AddressList.Length == 0)
                {
                    throw new Exception("Host entry has an empty address list");
                }

                var ip = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

                if (ip == null)
                {
                    throw new Exception("No IPV4 address found in the host entry's address list");
                }

                var endPoint = new IPEndPoint(ip, Uri.Port);
                var listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(endPoint);
                listener.Listen(MaxConnections);

                while (Running)
                {
                    AcceptConnections(listener);
                }
            }
            catch (Exception ex)
            {
                Running = false;
                OnServerError?.Invoke(ex);
            }
        }
        private void AcceptConnections(Socket listener)
        {
            try
            {
                var handler = listener.Accept();
                handler.ReceiveTimeout = ReceiveTimeout;
                handler.SendTimeout = SendTimeout;

                var buffer = new byte[BufferSize];
                var bytesReceived = handler.Receive(buffer);

                if (bytesReceived > 0)
                {
                    HandleNewClient(handler, buffer.Slice(0, bytesReceived));
                }
            }
            catch (Exception ex)
            {
                OnServerError?.Invoke(ex);
            }
        }
        private void HandleNewClient(Socket handler, byte[] data)
        {
            OnRequest?.Invoke(handler, data);
            
            try
            {
                // Parse the HTTP request
                var httpRequest = new HttpRequest(data, false);

                // Validate the http request
                if (httpRequest.Path != Uri.AbsolutePath)
                {
                    throw new HttpException(HttpStatusCode.NotFound, $"WebSocket server is not listening on path {httpRequest.Path} but on {Uri.AbsolutePath}");
                }
                if (string.IsNullOrEmpty(httpRequest.Headers.Connection))
                {
                    throw new HttpException(HttpStatusCode.BadRequest, "Header 'Connection' is missing or empty");
                }
                if (string.IsNullOrEmpty(httpRequest.Headers.Upgrade))
                {
                    throw new HttpException(HttpStatusCode.BadRequest, "Header 'Upgrade' is missing or empty");
                }
                if (string.IsNullOrEmpty(httpRequest.Headers.SecWebSocketKey))
                {
                    throw new HttpException(HttpStatusCode.BadRequest, "Header 'Sec-WebSocket-Key' is missing or empty");
                }
                if (httpRequest.Headers.Connection.Split(',').All(x => x.ToLower().Trim() != "upgrade"))
                {
                    throw new HttpException(HttpStatusCode.BadRequest, "Invalid connection header. Expected: upgrade");
                }
                if (httpRequest.Headers.Upgrade.ToLower() != "websocket")
                {
                    throw new HttpException(HttpStatusCode.BadRequest, "Invalid upgrade header. Expected: websocket");
                }

                // Generate the handshake http response and send it
                var httpResponse = new HttpResponse(handler);
                httpResponse.StatusCode = HttpStatusCode.SwitchingProtocols;
                httpResponse.Headers.Connection = "Upgrade";
                httpResponse.Headers.Upgrade = "websocket";
                httpResponse.Headers.SecWebSocketAccept = WebSocketUtils.GenerateResponseKey(httpRequest.Headers.SecWebSocketKey);
                httpResponse.Send(true);
                
                OnResponse?.Invoke(handler, httpResponse.GenerateRaw());
            }
            catch(HttpException ex)
            {
                var httpResponse = new HttpResponse(handler);
                httpResponse.StatusCode = ex.Status;
                httpResponse.Body = Encoding.ASCII.GetBytes(ex.Message);
                httpResponse.Send();
                OnResponse?.Invoke(handler, httpResponse.GenerateRaw());
                return;
            }
            catch (Exception ex)
            {
                var httpResponse = new HttpResponse(handler);
                httpResponse.StatusCode = HttpStatusCode.InternalServerError;
                httpResponse.Body = Encoding.ASCII.GetBytes("Something went wrong");
                httpResponse.Send();
                OnResponse?.Invoke(handler, httpResponse.GenerateRaw());
                OnServerError?.Invoke(ex);
                return;
            }

            try
            {
                // Create the client
                var client = new Client(StackSize);
                client.OnConnect += OnClientConnect;
                client.OnMessage += OnClientMessage;
                client.OnError += OnClientError;
                client.OnDisconnect += OnClientDisconnect;
                client.Connect(handler);

                // Add the client to our list of clients
                Clients.Add(client);
            }
            catch (Exception ex)
            {
                OnServerError?.Invoke(ex);
            }
        }

        private void UpdateThreadCallback()
        {
            // Update each 5 seconds
            while (Running)
            {
                try
                {
                    for (int i = 0; i < Clients.Count; i++)
                    {
                        var client = Clients[i];

                        // If the client is not connected anymore, remove it from the list
                        if (client.Status == WebSocketStatus.Closed)
                        {
                            Clients.Remove(client);
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnServerError?.Invoke(ex);
                }

                Thread.Sleep(10 * 1000);
            }
        }

        /* EVENTS */
        public delegate void OnRequestDelegate(Socket handler, byte[] data);
        public delegate void OnResponseDelegate(Socket handler, byte[] response);
        public delegate void OnClientConnectDelegate(Client client);
        public delegate void OnClientMessageDelegate(Client client, byte[] message);
        public delegate void OnClientErrorDelegate(Client client, Exception exception);
        public delegate void OnServerErrorDelegate(Exception exception);
        public delegate void OnClientDisconnectDelegate(Client client, WebSocketCloseStatus status, byte[] reason);
        
        public event OnRequestDelegate OnRequest;
        public event OnResponseDelegate OnResponse;
        public event OnClientConnectDelegate OnClientConnect;
        public event OnClientMessageDelegate OnClientMessage;
        public event OnClientErrorDelegate OnClientError;
        public event OnServerErrorDelegate OnServerError;
        public event OnClientDisconnectDelegate OnClientDisconnect;
    }
}