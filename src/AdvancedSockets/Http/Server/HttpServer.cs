using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdvancedSockets.Sockets;

namespace AdvancedSockets.Http.Server
{
    public class HttpServer
    {
        private int receiveTimeout;
        private int sendTimeout;
        private int maxConnections;
        private int bufferSize;
        private IPEndPoint endPoint;

        public int ReceiveTimeout
        {
            get => receiveTimeout;
            set => receiveTimeout = value;
        }

        public int SendTimeout
        {
            get => sendTimeout;
            set => sendTimeout = value;
        }

        public int MaxConnections
        {
            get => maxConnections;
            set => maxConnections = value;
        }

        public int BufferSize
        {
            get => bufferSize;
            set => bufferSize = value;
        }

        public IPEndPoint EndPoint
        {
            get => endPoint;
            set => endPoint = value;
        }

        private HttpServer()
        {
            receiveTimeout = 1000 * 30;
            sendTimeout = 1000 * 30;
            maxConnections = 1000;
            bufferSize = 1024 * 1024; // 1MB
        }
        public HttpServer(int port) : this(Dns.GetHostName(), 8080)
        {

        }
        public HttpServer(string host, int port) : this()
        {
            var hostEntry = Dns.GetHostEntry(host);
            
            if (hostEntry.AddressList.Length == 0)
            {
                throw new Exception("Host entry has an empty address list");
            }

            var ip = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

            if (ip == null)
            {
                throw new Exception("No IPV4 address found in the host entry's address list");
            }

            this.endPoint = new IPEndPoint(ip, port);
        }
        public HttpServer(IPEndPoint endPoint) : this()
        {
            this.endPoint = endPoint;
        }

        public void Start()
        {
            // Create a TCP/IP socket
            var listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Start listening to incoming requests
            listener.Bind(endPoint);
            listener.Listen(maxConnections);

            while (true)
            {
                try
                {
                    var handler = listener.Accept();
                    handler.ReceiveTimeout = receiveTimeout;
                    handler.SendTimeout = sendTimeout;
                    
                    HandleRequest(handler);
                }
                catch (Exception ex)
                {
                    OnException?.Invoke(ex);
                }
            }
        }
        private void HandleRequest(Socket handler)
        {
            var data = FetchFullRequest(handler);
            var info = new HttpConnectionInfo(handler);
            var response = new HttpResponse(handler);
            var request = new HttpRequest(data);
            
            try
            {
                OnRequestStart?.Invoke(request, info);
                OnRequest?.Invoke(request, response, info);
                OnRequestEnd?.Invoke(request, response, info);
            }
            catch (HttpException ex)
            {
                OnHttpError?.Invoke(ex.Status, ex.Message, request, response, info);
            }
            catch (Exception ex)
            {
                OnHttpError?.Invoke(HttpStatusCode.InternalServerError, "Something went wrong", request, response, info);
                OnException?.Invoke(ex);
            }
        }

        private byte[] FetchFullRequest(Socket handler, byte[] data = null)
        {
            if (data == null)
            {
                data = new byte[0];
            }
            
            var fullRequestReceived = false;
            var buffer = new byte[bufferSize];
            var bytesReceived = handler.Receive(buffer);
            var bytes = buffer.Slice(0, bytesReceived);
                
            if (bytesReceived > 0)
            {
                data = data.Push(bytes);
            }
            else
            {
                // If no bytes are sent anymore it means the connection got abruptly closed from the client
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

                throw new Exception("Client stopped sending bytes");
            }

            // Try and parse what we already got to see if the full request was received in bytes
            try
            {
                var topLength = 0;
                var bodyLength = 0;
                var ascii = Encoding.ASCII.GetString(data).Replace("\r", "");
                var lines = ascii.Split('\n');

                // We don't parse all lines entirely
                // We only fetch what we need
                foreach (var line in lines)
                {
                    var lower = line.ToLower();

                    // Increase the toplength with the line's length + 2
                    // The +2 resembles the length of the carriage return and newline symbol
                    // But since we split the lines these are stripped away from it
                    topLength += line.Length + 2;

                    // Locate the content-length header to know how big the body is
                    // We cannot count this as the body may be non-ASCII bytes
                    // 
                    // We are also going to assume for now it is present
                    // Most modern HTTP clients are setup to do this correctly so I find it of less importance to add a check for it right now
                    if (lower.StartsWith("content-length"))
                    {
                        bodyLength = int.Parse(lower.Replace("content-length: ", ""));
                    }

                    // An empty line marks the end of the headers and the begin of the body
                    // This is where we stop
                    if (line.Length == 0)
                    {
                        break;
                    }
                }

                // Now we check if the data length equals the top length + the body length
                if (data.Length == topLength + bodyLength)
                {
                    fullRequestReceived = true;
                }
            }
            catch (Exception)
            {
                // Just ignore the exception
            }

            if (!fullRequestReceived)
            {
                data = FetchFullRequest(handler, data);
            }

            return data;
        }

        /* EVENTS */
        public delegate void OnRequestDelegate(HttpRequest request, HttpResponse response, HttpConnectionInfo info);
        public delegate void OnRequestStartDelegate(HttpRequest request, HttpConnectionInfo connectionInfo);
        public delegate void OnRequestEndDelegate(HttpRequest request, HttpResponse response, HttpConnectionInfo connectionInfo);
        public delegate void OnExceptionDelegate(Exception ex);
        public delegate void OnHttpErrorDelegate(HttpStatusCode status, string error, HttpRequest request, HttpResponse response, HttpConnectionInfo info);
        public event OnRequestDelegate OnRequest;
        public event OnRequestStartDelegate OnRequestStart;
        public event OnRequestEndDelegate OnRequestEnd;
        public event OnExceptionDelegate OnException;
        public event OnHttpErrorDelegate OnHttpError;
    }
}