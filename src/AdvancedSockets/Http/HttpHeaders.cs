using System;
using System.Collections.Generic;
using System.Text;

namespace AdvancedSockets.Http
{
    public class HttpHeaders
    {
        private Dictionary<string, string> headers;

        // Pre-defined
        public string AccessControlAllowHeaders
        {
            get { return this["access-control-allow-headers"]; }
            set { this["access-control-allow-headers"] = value; }
        }
        public string AccessControlAllowOrigin
        {
            get { return this["access-control-allow-origin"]; }
            set { this["access-control-allow-origin"] = value; }
        }
        public string AccessControlAllowMethods
        {
            get { return this["access-control-allow-methods"]; }
            set { this["access-control-allow-methods"] = value; }
        }
        public string AccessControlAllowCredentials
        {
            get { return this["access-control-allow-credentials"]; }
            set { this["access-control-allow-credentials"] = value; }
        }
        public string Accept
        {
            get { return this["accept"]; }
            set { this["accept"] = value; }
        }
        public string AcceptEncoding
        {
            get { return this["accept-encoding"]; }
            set { this["accept-encoding"] = value; }
        }
        public string Authorization
        {
            get { return this["authorization"]; }
            set { this["authorization"] = value; }
        }
        public string CacheControl
        {
            get { return this["cache-control"]; }
            set { this["cache-control"] = value; }
        }
        public string Connection
        {
            get { return this["connection"]; }
            set { this["connection"] = value; }
        }
        public int ContentLength
        {
            get
            {
                string value = this["content-length"];

                if (value == null)
                {
                    return 0;
                }
                else
                {
                    return int.Parse(value);
                }
            }
            internal set
            {
                this["content-length"] = value.ToString();
            }
        }
        public string ContentType
        {
            get { return this["content-type"]; }
            set { this["content-type"] = value; }
        }
        public string ContentEncoding
        {
            get { return this["content-encoding"]; }
            set { this["content-encoding"] = value; }
        }
        public string Host
        {
            get { return this["host"]; }
            set { this["host"] = value; }
        }
        public string Location
        {
            get { return this["location"]; }
            set { this["location"] = value; }
        }
        public string Origin
        {
            get { return this["origin"]; }
            set { this["origin"] = value; }
        }
        public string Upgrade
        {
            get { return this["upgrade"]; }
            set { this["upgrade"] = value; }
        }
        public string UserAgent
        {
            get { return this["user-agent"]; }
            set { this["user-agent"] = value; }
        }
        public string SecWebSocketAccept
        {
            get { return this["sec-websocket-accept"]; }
            set { this["sec-websocket-accept"] = value; }
        }
        public string SecWebSocketKey
        {
            get { return this["sec-websocket-key"]; }
            set { this["sec-websocket-key"] = value; }
        }
        public string SecWebSocketVersion
        {
            get { return this["sec-websocket-version"]; }
            set { this["sec-websocket-version"] = value; }
        }
        public string SecWebSocketProtocol
        {
            get { return this["sec-websocket-protocol"]; }
            set { this["sec-websocket-protocol"] = value; }
        }
        public string Server
        {
            get { return this["server"]; }
            set { this["server"] = value; }
        }

        public string this[string key]
        {
            get
            {
                if (headers.ContainsKey(key.ToLower()))
                {
                    return headers[key.ToLower()];
                }

                return null;
            }
            set
            {
                if (this[key] == null)
                {
                    headers.Add(key.ToLower(), value);
                }
                else
                {
                    headers[key.ToLower()] = value;
                }
            }
        }

        internal HttpHeaders()
        {
            headers = new Dictionary<string, string>();
        }

        public void Add(string name, string value)
        {
            if (this[name] != null)
            {
                throw new InvalidOperationException("Header already exists");
            }

            headers.Add(name.ToLower(), value.Replace("\r", "").Replace("\n", ""));
        }
        public bool Contains(string name)
        {
            return this[name] != null;
        }

        public override string ToString()
        {
            string result = "";

            foreach (var header in headers)
            {
                result += header.Key + ": " + header.Value + "\r\n";
            }

            return result;
        }
        public List<string> ToList()
        {
            List<string> result = new List<string>();

            foreach (var header in headers)
            {
                result.Add(header.Key + ": " + header.Value);
            }

            return result;
        }
    }
}
