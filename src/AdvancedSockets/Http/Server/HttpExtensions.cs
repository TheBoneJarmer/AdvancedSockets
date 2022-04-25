using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AdvancedSockets.Http.Server
{
    public static class HttpExtensions
    {
        public static void Send(this HttpResponse response, bool keepAlive = false)
        {
            // Add required headers
            if (response.Headers.ContentType == null)
            {
                response.Headers.ContentType = "text/plain";
            }
            if (response.Headers.Server == null)
            {
                response.Headers.Server = "AdvancedSockets Http Server";
            }

            response.Headers.ContentLength = response.Body == null ? 0 : response.Body.Length;

            // Send the data
            response.Socket.Send(response.GenerateRaw());

            if (!keepAlive)
            {
                response.Socket.Shutdown(SocketShutdown.Both);
                response.Socket.Close();
            }
        }

        public static void SendFile(this HttpResponse response, string path, bool keepAlive = false)
        {
            if (!File.Exists(path))
            {
                throw new HttpException(HttpStatusCode.NotFound, $"{path} not found");
            }

            response.Headers.ContentType = HttpUtils.GetContentType(path);
            response.Headers.ContentLength = (int)new FileInfo(path).Length;
            response.Body = File.ReadAllBytes(path);
            response.Send(keepAlive);
        }
    }
}