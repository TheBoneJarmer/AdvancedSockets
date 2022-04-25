using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AdvancedSockets.Http;
using AdvancedSockets.Http.Client;

namespace AdvancedSockets.WebSockets
{
    public class WebSocketClient
    {
        private Socket socket;
        private Thread thread;
        
        public WebSocketStatus Status { get; private set; }
        public int ReceiveTimeout { get; private set; }
        public int SendTimeout { get; private set; }

        public WebSocketClient() : this(1000 * 60 * 30)
        {

        }
        public WebSocketClient(int receiveTimeout) : this(receiveTimeout, 1000 * 30)
        {

        }
        public WebSocketClient(int receiveTimeout, int sendTimeout)
        {
            Status = WebSocketStatus.Opening;
            ReceiveTimeout = receiveTimeout;
            SendTimeout = sendTimeout;
        }

        public void Connect(Uri uri)
        {
            if (uri.Scheme != "ws" && uri.Scheme != "wss")
            {
                OnError?.Invoke($"Wrong protocol scheme for url {uri}. Expected ws:// or wss://", null);
                return;
            }
            
            try
            {
                var key = WebSocketUtils.GenerateRequestKey();
                var socket = CreateSocket(uri);

                if (uri.Scheme == "ws")                
                {
                    SendPlainHandshake(uri, key);
                }
                else
                {
                    SendSecureHandshake(uri, key);
                }
            }
            catch (SocketException ex)
            {
                var reasonString = "Unable to connect to server";
                var reasonBytes = Encoding.ASCII.GetBytes(reasonString);

                Status = WebSocketStatus.Closed;
                OnDisconnect?.Invoke(WebSocketCloseStatus.NormalClosure, reasonBytes);
                OnError?.Invoke("A socket error occured during handshake", ex);
            }
            catch (Exception ex)
            {
                var reasonString = "A server error occured";
                var reasonBytes = Encoding.ASCII.GetBytes(reasonString);

                Status = WebSocketStatus.Closed;
                OnDisconnect?.Invoke(WebSocketCloseStatus.NormalClosure, reasonBytes);
                OnError?.Invoke("A runtime error occured during handshake", ex);
            }
        }

        private Socket CreateSocket(Uri uri)
        {
            var ipHostEntry = Dns.GetHostEntry(uri.Host);

            if (ipHostEntry.AddressList.Length == 0)
            {
                throw new Exception("Host entry has an empty address list");
            }

            var ipAddress = ipHostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

            if (ipAddress == null)
            {
                throw new Exception("No IPV4 address found in the host entry's address list");
            }

            var ipEndpoint = new IPEndPoint(ipAddress, uri.Port);

            // Create the client socket
            socket = new Socket(ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = ReceiveTimeout;
            socket.SendTimeout = SendTimeout;
            socket.Connect(ipEndpoint);

            return socket;
        }

        private void SendSecureHandshake(Uri uri, string key)
        {
            var data = new List<byte>();
            var networkStream = new NetworkStream(socket);
            var sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

            // Authenticate first
            try
            {
                sslStream.AuthenticateAsClient(uri.Host);
            }
            catch (AuthenticationException ex)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                
                throw new SslException($"Authentication with server failed", ex);
            }

            // Generate our request
            var request = new HttpRequest();
            request.Socket = socket;
            request.Method = HttpMethod.Get;
            request.Path = uri.AbsolutePath;
            request.Headers.Host = uri.Host;
            request.Headers.Upgrade = "websocket";
            request.Headers.Connection = "keep-alive, upgrade";
            request.Headers.Origin = Dns.GetHostName();
            request.Headers.SecWebSocketKey = key;
            request.Headers.SecWebSocketProtocol = "chat, superchat";
            request.Headers.SecWebSocketVersion = "13";
            
            // Send our request bytes
            sslStream.Write(request.GenerateRaw());
            sslStream.Flush();

            while (true)
            {
                var buffer = new byte[1024];
                var received = sslStream.Read(buffer, 0, buffer.Length);
                
                data.AddRange(buffer.Slice(0, received));

                if (received < buffer.Length)
                {
                    break;
                }
            }

            if (data.Count > 0)
            {
                var httpResponse = new HttpResponse(data.ToArray());

                ValidateHandshakeResponse(httpResponse, key);

                // Start the thread
                thread = new Thread(Thread_Callback);
                thread.Start();
            }
        }

        private void SendPlainHandshake(Uri uri, string key)
        {
            var buffer = new byte[1024];

            var httpRequest = new HttpRequest();
            httpRequest.Socket = socket;
            httpRequest.Method = HttpMethod.Get;
            httpRequest.Path = uri.AbsolutePath;
            httpRequest.Headers.Host = uri.Host;
            httpRequest.Headers.Upgrade = "websocket";
            httpRequest.Headers.Connection = "keep-alive, upgrade";
            httpRequest.Headers.Origin = Dns.GetHostName();
            httpRequest.Headers.SecWebSocketKey = key;
            httpRequest.Headers.SecWebSocketProtocol = "chat, superchat";
            httpRequest.Headers.SecWebSocketVersion = "13";
            httpRequest.Socket.Send(httpRequest.GenerateRaw());

            var received = socket.Receive(buffer);

            if (received > 0)
            {
                var data = buffer.Slice(0, received);
                var httpResponse = new HttpResponse(data);

                ValidateHandshakeResponse(httpResponse, key);

                // Start the thread
                thread = new Thread(Thread_Callback);
                thread.Start();
            }
        }

        private void ValidateHandshakeResponse(HttpResponse httpResponse, string key)
        {
            if (httpResponse.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                if (httpResponse.StatusCode == HttpStatusCode.Moved)
                {
                    throw new Exception($"Server endpoint moved to {httpResponse.Headers.Location}");
                }
                if (httpResponse.StatusCode == HttpStatusCode.Redirect)
                {
                    throw new Exception($"Server endpoint redirects to {httpResponse.Headers.Location}");
                }
                if ((int)httpResponse.StatusCode >= 400)
                {
                    throw new Exception($"Server returned {(int)httpResponse.StatusCode}: {Encoding.ASCII.GetString(httpResponse.Body)}");
                }

                throw new Exception($"Server returned {(int)httpResponse.StatusCode} instead of 101");
            }
            if (httpResponse.Headers.SecWebSocketAccept == null)
            {
                throw new Exception("Header 'sec-websocket-accept' missing");
            }
            if (httpResponse.Headers.Connection == null)
            {
                throw new Exception("Header 'connection' missing");
            }
            if (httpResponse.Headers.Upgrade == null)
            {
                throw new Exception("Header 'upgrade' missing");
            }
            if (httpResponse.Headers.Connection.ToLower() != "upgrade")
            {
                throw new Exception($"Server provided '{httpResponse.Headers.Connection}' for header 'connection'. Expected 'upgrade'");
            }
            if (httpResponse.Headers.Upgrade.ToLower() != "websocket")
            {
                throw new Exception($"Server provided '{httpResponse.Headers.Upgrade}' for header 'upgrade'. Expected 'websocket'");
            }
            if (httpResponse.Headers.SecWebSocketAccept != WebSocketUtils.GenerateResponseKey(key))
            {
                throw new Exception("Server provided wrong accept key in header 'sec-websocket-accept'");
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                throw new SslException($"SSL policy error occured while validating server certificate. Error was of type {sslPolicyErrors}");
            }

            return true;
        }

        public void CloseConnection()
        {
            CloseConnection(WebSocketCloseStatus.NormalClosure, new byte[0]);
        }
        public void CloseConnection(string reason)
        {
            CloseConnection(WebSocketCloseStatus.NormalClosure, Encoding.ASCII.GetBytes(reason));
        }
        public void CloseConnection(byte[] reason)
        {
            CloseConnection(WebSocketCloseStatus.NormalClosure, reason);
        }
        internal void CloseConnection(WebSocketCloseStatus status, byte[] reason)
        {
            if (reason == null)
            {
                reason = new byte[0];
            }

            try
            {
                var statusBinary = ((int)status).IntToBinaryString().PadLeft(16, '0');
                var data = new List<byte>();
                data.Add(statusBinary.Substring(0, 8).BinaryStringToByte());
                data.Add(statusBinary.Substring(8, 8).BinaryStringToByte());
                data.AddRange(reason);

                var message = new WebSocketMessage(data.ToArray(), WebSocketOpcode.Closing, true);
                var result = message.Encode();

                socket.Send(result);
                Status = WebSocketStatus.Closing;
            }
            catch (Exception ex)
            {
                Status = WebSocketStatus.Closed;
                OnError?.Invoke("An error occured while closing the connection", ex);
            }
        }

        public void AbortConnection()
        {
            try
            {
                if (socket != null)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke("An error occured while aborting the connection", ex);
            }
            finally
            {
                Status = WebSocketStatus.Closed;
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                var message = new WebSocketMessage(data, WebSocketOpcode.Text, true);
                var result = message.Encode();

                socket.Send(result);
            }
            catch (Exception ex)
            {
                Status = WebSocketStatus.Closed;
                OnError?.Invoke("An error occured while sending a message to the server", ex);
            }
        }

        /* THREADING */
        private void Thread_Callback()
        {
            var buffer = new byte[WebSocketConstants.MAX_BUFFER_SIZE];
            var data = new byte[0];

            var closeStatus = WebSocketCloseStatus.NormalClosure;
            var closeReason = new byte[0];

            try
            {
                OnConnect?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke("An error occured during the OnConnect event", ex);
            }

            try
            {
                Status = WebSocketStatus.Open;
                
                while(Status != WebSocketStatus.Closed)
                {
                    int bytesReceived = socket.Receive(buffer);

                    // If the client still has bytes, add them to our data buffer
                    // If no bytes were received it means the client closed the connection roughly
                    if (bytesReceived > 0)
                    {
                        data = data.Push(buffer.Slice(0, bytesReceived));
                    }
                    else
                    {
                        Status = WebSocketStatus.Closed;
                        break;
                    }

                    while (data.Length > 0)
                    {
                        var message = new WebSocketMessage(data);
                        data = message.Decode();

                        // If the message is not fully received wait a bit longer
                        if (message.Incomplete)
                        {
                            break;
                        }

                        // Do not allow the buffer length to exceed the max buffer size
                        if ((uint)data.Length > WebSocketConstants.MAX_BUFFER_SIZE)
                        {
                            var reasonString = $"Buffer length exceeds the maximum of {WebSocketConstants.MAX_BUFFER_SIZE} bytes";
                            var reasonBytes = Encoding.ASCII.GetBytes(reasonString);

                            CloseConnection(WebSocketCloseStatus.BufferOverflow, reasonBytes);
                            break;
                        }

                         // Otherwise handle the received data per type
                        if (message.Opcode == WebSocketOpcode.Text)
                        {
                            OnMessage?.Invoke(message.Message);
                        }

                        if (message.Opcode == WebSocketOpcode.Closing)
                        {
                            var binary1 = message.Message[0].ByteToBinaryString();
                            var binary2 = message.Message[1].ByteToBinaryString();
                            var binary = binary1 + binary2;
                            
                            closeStatus = (WebSocketCloseStatus)binary.BinaryStringToInt();
                            closeReason = message.Message.Slice(2);

                            if (Status == WebSocketStatus.Open)
                            {
                                CloseConnection();
                            }
                            
                            Status = WebSocketStatus.Closed;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke("An error occured while receiving data from the server", ex);
                Status = WebSocketStatus.Closed;
                
                closeStatus = WebSocketCloseStatus.NormalClosure;
                closeReason = Encoding.ASCII.GetBytes("A server error occured");
            }

            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception ex)
            {
                OnError?.Invoke("An error occured while trying to shutdown the socket", ex);
            }

            try
            {
                OnDisconnect?.Invoke(closeStatus, closeReason);
            }
            catch (Exception ex)
            {
                OnError?.Invoke("An error occured in the OnDisconnect event", ex);
            }
        }

        /* EVENTS */
        public delegate void OnConnectDelegate();
        public delegate void OnDisconnectDelegate(WebSocketCloseStatus status, byte[] reason);
        public delegate void OnMessageDelegate(byte[] message);
        public delegate void OnErrorDelegate(string error, Exception ex);

        public event OnConnectDelegate OnConnect;
        public event OnDisconnectDelegate OnDisconnect;
        public event OnMessageDelegate OnMessage;
        public event OnErrorDelegate OnError;
    }
}