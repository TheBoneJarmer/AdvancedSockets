using System;
using System.Net;
using AdvancedSockets;

namespace AdvancedSockets.Api
{
    public class ApiResponse
    {
        public HttpStatusCode Status { get; private set; }
        public string Body { get; private set; }

        internal ApiResponse(HttpStatusCode statusCode, string body)
        {
            this.Status = statusCode;
            this.Body = body;
        }

        public void ThrowExceptionWhenBadStatus()
        {
            if ((int)Status >= 400)
            {
                throw new HttpException(Status, Body);
            }
        }
    }
}
