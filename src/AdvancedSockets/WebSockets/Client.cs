using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace AdvancedSockets.WebSockets
{
    public class Client
    {
        private Thread thread;
        private Socket socket;
        private int stackSize;

        public IPAddress IpAddress { get; private set; }
        public string Id { get; private set; }
        public WebSocketStatus Status { get; private set; }
        
        [Obsolete("Use property Storage instead")]
        public Dictionary<string, string> Properties { get; private set; }
        public object Storage { get; set; }

        internal Client(int stackSize)
        {
            this.stackSize = stackSize;
            
            Id = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, string>();
            Status = WebSocketStatus.Opening;
        }

        public void Connect(Socket socket)
        {
            try
            {
                this.socket = socket;
                this.IpAddress = ((IPEndPoint) (socket.RemoteEndPoint)).Address;

                // Create the thread and start it
                thread = new Thread(ThreadCallback, stackSize);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                Status = WebSocketStatus.Closed;
                OnError?.Invoke(this, ex);
            }
        }

        public void Send(byte[] data)
        {
            if (Status != WebSocketStatus.Open)
            {
                throw new WebSocketException("Unable to send data. WebSocket is not open");
            }

            try
            {
                var message = new WebSocketMessage(data, WebSocketOpcode.Text, false);
                var result = message.Encode();

                socket.Send(result);
            }
            catch (Exception ex)
            {
                Status = WebSocketStatus.Closed;
                OnError?.Invoke(this, ex);
            }
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

                var message = new WebSocketMessage(data.ToArray(), WebSocketOpcode.Closing, false);
                var result = message.Encode();

                socket.Send(result);
                Status = WebSocketStatus.Closing;
            }
            catch (Exception ex)
            {
                Status = WebSocketStatus.Closed;
                OnError?.Invoke(this, ex);
            }
        }

        public void AbortConnection()
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception)
            {
                // Ignore the exception
            }
            
            Status = WebSocketStatus.Closed;
        }

        public long Ping()
        {
            var ping = new Ping();
            var reply = ping.Send(IpAddress);

            if (reply.Status == IPStatus.Success)
            {
                return reply.RoundtripTime;
            }

            return -1;
        }

        private void ThreadCallback()
        {
            var buffer = new byte[WebSocketConstants.MAX_BUFFER_SIZE];
            var data = new byte[0];
            var closeStatus = WebSocketCloseStatus.NormalClosure;
            var closeReason = new byte[0];
            
            try
            {
                OnConnect?.Invoke(this);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
            
            try
            {
                Status = WebSocketStatus.Open;

                // Go in a loop to wait for incoming messages
                while (Status != WebSocketStatus.Closed)
                {
                    int bytesReceived = socket.Receive(buffer);

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
                        
                        // Break the while loop if the message is incomplete
                        if (message.Incomplete)
                        {
                            break;
                        }

                        // IETF RFC 6455 5.3
                        // Clients are required to mask their bytes, if they did not the server has to send a close frame with a 1002 status as body
                        if (!message.Masked)
                        {
                            var reasonString = "Invalid WebSocket frame: MASK must be set.";
                            var reasonBytes = Encoding.ASCII.GetBytes(reasonString);

                            CloseConnection(WebSocketCloseStatus.ProtocolError, reasonBytes);
                            break;
                        }

                        if (message.Opcode == WebSocketOpcode.Text)
                        {
                            OnMessage?.Invoke(this, message.Message);
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
                Status = WebSocketStatus.Closed;
                OnError?.Invoke(this, ex);
            }

            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }

            try
            {
                OnDisconnect?.Invoke(this, closeStatus, closeReason);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
        }

        /* EVENTS */
        public event WebSocketServer.OnClientConnectDelegate OnConnect;
        public event WebSocketServer.OnClientMessageDelegate OnMessage;
        public event WebSocketServer.OnClientErrorDelegate OnError;
        public event WebSocketServer.OnClientDisconnectDelegate OnDisconnect;
    }
}