using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace AdvancedSockets.Http.Client
{
    public class HttpClient
    {
        private Socket socket;

        public int ReceiveTimeout { get; private set; }
        public int SendTimeout { get; private set; }

        public HttpClient() : this(1000 * 10)
        {

        }
        public HttpClient(int receiveTimeout) : this(receiveTimeout, 1000 * 10)
        {

        }
        public HttpClient(int receiveTimeout, int sendTimeout)
        {
            ReceiveTimeout = receiveTimeout;
            SendTimeout = sendTimeout;
        }

        private Socket Connect(HttpClientRequest request)
        {
            var ipHostEntry = Dns.GetHostEntry(request.host);
            var ipAddress = ipHostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            var ipEndpoint = new IPEndPoint(ipAddress, request.port);

            if (ipHostEntry.AddressList.Length == 0)
            {
                throw new Exception("Host entry has an empty address list");
            }
            if (ipAddress == null)
            {
                throw new Exception("No IPV4 address found in the host entry's address list");
            }

            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = SendTimeout;
            socket.ReceiveTimeout = ReceiveTimeout;
            
            socket.Connect(ipEndpoint);

            return socket;
        }

        private HttpResponse SendPlain(HttpClientRequest request, Socket socket)
        {
            var data = new List<byte>();

            // Send our plain request
            socket.Send(request.request.GenerateRaw());

            // Wait for a reply
            while (true)
            {
                var buffer = new byte[1024];
                var received = socket.Receive(buffer);
                
                data.AddRange(buffer.Slice(0, received));

                if (received < buffer.Length)
                {
                    break;
                }
            }

            if (data.Count > 0)
            {
                return new HttpResponse(data.ToArray());
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            return null;
        }
        private HttpResponse SendSecure(HttpClientRequest request, Socket socket)
        {
            var data = new List<byte>();
            var networkStream = new NetworkStream(socket);
            var sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

            // Authenticate first
            try
            {
                sslStream.AuthenticateAsClient(request.host);
            }
            catch (AuthenticationException ex)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                
                throw new SslException($"Authentication with server failed", ex);
            }

            // Send our request bytes
            sslStream.Write(request.request.GenerateRaw());
            sslStream.Flush();

            // Wait for a reply
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

            // Close the socket connection as we no longer need it
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            if (data.Count > 0)
            {
                return new HttpResponse(data.ToArray());
            }

            return null;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                throw new SslException($"SSL policy error occured while validating server certificate. Error was of type {sslPolicyErrors}");
            }

            return true;
        }

        public HttpResponse Send(HttpClientRequest request)
        {
            var socket = Connect(request);

            if (request.secure)            
            {
                return SendSecure(request, socket);
            }
            else
            {
                return SendPlain(request, socket);
            }
        }
    }
}