using System;
using System.Net;
using System.Net.Http;

namespace AdvancedSockets.Http.Client
{
    public class HttpClientRequest
    {
        internal string host;
        internal int port;
        internal HttpRequest request;
        internal bool secure;

        public HttpMethod Method
        {
            get { return request.Method; }
            set { request.Method = value; }
        }
        public HttpHeaders Headers
        {
            get { return request.Headers; }
            set { request.Headers = value; }
        }
        public HttpCookies Cookies
        {
            get { return request.Cookies; }
            set { request.Cookies = value; }
        }
        public HttpBody Body
        {
            get { return request.Body; }
            set { request.Body = value; }
        }

        public HttpClientRequest(Uri uri)
        {
            host = uri.Host;
            port = uri.Port;
            secure = uri.Scheme == "https";

            GenerateRequest(uri);
        }
        private void GenerateRequest(Uri uri)
        {
            request = new HttpRequest();
            request.Path = uri.AbsolutePath;
            request.Method = HttpMethod.Get;
            request.Headers.Host = $"{uri.Host}:{uri.Port}";
            request.Headers.UserAgent = "AdvancedSockets Http Client";

            if (!string.IsNullOrEmpty(uri.Query))
            {
                var query = new HttpQuery();
                var keyValues = uri.Query.Substring(1).Split('&');

                foreach (var entry in keyValues)
                {
                    var key = entry.Split('=')[0];
                    var value = entry.Split('=')[1];

                    query.Add(key, value);
                }

                request.Query = query;
            }
        }
    }
}