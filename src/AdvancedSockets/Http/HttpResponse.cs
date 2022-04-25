using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace AdvancedSockets.Http
{
    public class HttpResponse
    {
        public HttpCookies Cookies { get; set; }
        public HttpHeaders Headers { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public byte[] Body { get; set; }

        internal bool AllHeadersReceived { get; set; }
        internal Socket Socket { get; set; }

        private HttpResponse()
        {
            StatusCode = HttpStatusCode.OK;
            Headers = new HttpHeaders();
            Cookies = new HttpCookies();
        }
        internal HttpResponse(Socket socket) : this()
        {
            Socket = socket;
        }
        internal HttpResponse(byte[] data) : this()
        {
            Parse(data);
        }

        private void Parse(byte[] data)
        {
            try
            {
                var body = new List<byte>();
                var line = "";

                foreach (var b in data)
                {
                    char c = (char)b;

                    // Add the character to the line
                    line += c;

                    // Check if the line indicates the start of the body
                    if (line == "\r\n" && !AllHeadersReceived)
                    {
                        AllHeadersReceived = true;
                        continue;
                    }

                    // Parse the line if the line is complete
                    if (line.EndsWith("\r\n") && !AllHeadersReceived)
                    {
                        if (Regex.IsMatch(line, @"^HTTP/"))
                        {
                            StatusCode = (HttpStatusCode)int.Parse(line.Split(' ')[1]);
                        }
                        else
                        {
                            string headerName = line.Substring(0, line.IndexOf(":"));
                            string headerValue = line.Substring(headerName.Length + 2).Replace("\r\n", "");

                            Headers.Add(headerName, headerValue);
                        }

                        line = "";
                    }

                    // If the headers are done, start with the body
                    if (AllHeadersReceived)
                    {
                        body.Add(b);
                    }
                }

                Body = body.ToArray();
            }
            catch (HttpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FormatException("Unable to parse HTTP request", ex);
            }
        }

        public byte[] GenerateRaw()
        {
            var result = new List<byte>();
            var crlf = Encoding.ASCII.GetBytes("\r\n");

            result.AddRange(Encoding.ASCII.GetBytes($"HTTP/1.1 {(int)StatusCode} {HttpUtils.GetStatusText(StatusCode)}"));
            result.AddRange(crlf);
            result.AddRange(Encoding.ASCII.GetBytes(Headers.ToString()));
            
            if (Cookies.Count > 0)
            {
                result.AddRange(Encoding.ASCII.GetBytes($"set-cookie: {Cookies.ToString()}"));
                result.AddRange(crlf);
            }

            result.AddRange(crlf);

            if (Body != null)
            {
                result.AddRange(Body);
            }

            return result.ToArray();
        }
    }
}