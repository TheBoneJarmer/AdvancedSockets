using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Text;

namespace AdvancedSockets
{
    public class SslException : Exception
    {
        public SslException(string message) : base(message)
        {
            
        }
        public SslException(string message, Exception inner) : base(message, inner)
        {
            
        }
    }

    public class WebSocketException : Exception
    {
        public WebSocketException(string message) : base(message)
        {

        }
    }
    
    public class HttpException : Exception
    {
        public HttpStatusCode Status { get; private set; }
        public string Response { get; private set; }

        public HttpException(string response) : base(response)
        {
            this.Status = HttpStatusCode.BadRequest;
            this.Response = response;
        }
        public HttpException(HttpStatusCode status, string responseText) : base(responseText)
        {
            this.Status = status;
            this.Response = responseText;
        }
    }
}
