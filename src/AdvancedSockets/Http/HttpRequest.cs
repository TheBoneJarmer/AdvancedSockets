using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace AdvancedSockets.Http
{
    public class HttpRequest
    {
        public Encoding CharSet { get; set; }

        public HttpMethod Method { get; set; }
        public HttpHeaders Headers { get; set; }
        public HttpCookies Cookies { get; set; }
        public HttpQuery Query { get; set; }
        public HttpBody Body { get; set; }
        public string Path { get; set; }

        internal bool AllHeadersReceived { get; set; }
        internal Socket Socket { get; set; }
        
        internal HttpRequest()
        {
            Headers = new HttpHeaders();
            Cookies = new HttpCookies();
            
            CharSet = Encoding.UTF8;
        }
        internal HttpRequest(byte[] data, bool parseBody = true) : this()
        {
            Parse(data, parseBody);
        }

        private void Parse(byte[] data, bool parseBody)
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
                        ParseLine(line);
                        line = "";
                    }

                    // If the headers are done, start with the body
                    if (AllHeadersReceived)
                    {
                        body.Add(b);
                    }
                }

                if (parseBody)
                {
                    ParseBody(body);
                }

                FixHeaders();
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

        private void ParseLine(string line)
        {
            // Check if the line is the method line
            if (Regex.IsMatch(line, @"^(GET|POST|PUT|DELETE|OPTIONS)"))
            {
                ParseMethodAndPath(line);
            }
            else
            {
                ParseHeader(line);
            }
        }

        private void ParseMethodAndPath(string line)
        {
            string method = line.Split(' ')[0];
            string path = line.Split(' ')[1];
            string version = line.Split(' ')[2];

            // Set the method
            if (method == "GET") this.Method = HttpMethod.Get;
            if (method == "POST") this.Method = HttpMethod.Post;
            if (method == "PUT") this.Method = HttpMethod.Put;
            if (method == "DELETE") this.Method = HttpMethod.Delete;
            if (method == "OPTIONS") this.Method = HttpMethod.Options;

            // Set the path
            Path = HttpUtils.ConvertUrlEncoding(path);

            // Convert part of the path to query if the method is GET
            if (path.Contains("?"))
            {
                string actualPath = path.Split('?')[0];
                string queryString = path.Split('?')[1];

                // Reset the path
                Path = actualPath;

                // Fill up the query
                foreach (string keyvalue in queryString.Split('&'))
                {
                    string key = keyvalue.Split('=')[0];
                    string value = keyvalue.Split('=')[1];

                    if (Query == null)
                    {
                        Query = new HttpQuery();
                    }

                    Query.Add(key, HttpUtils.ConvertUrlEncoding(value));
                }
            }
        }

        private void ParseHeader(string line)
        {
            string headerName = line.Substring(0, line.IndexOf(":"));
            string headerValue = line.Substring(headerName.Length + 2).Replace("\r\n", "");

            // The cookie header's values are key value pairs as well, so therefore the seperate dictionary
            if (headerName.ToLower() == "cookie")
            {
                string[] cookies = headerValue.Split(';');

                foreach (var cookie in cookies)
                {
                    string cookieName = cookie.Replace(" ", "").Split('=')[0].Trim();
                    string cookieValue = cookie.Replace(" ", "").Split('=')[1].Trim();

                    Cookies.Add(cookieName, cookieValue);
                }
            }

            Headers.Add(headerName, headerValue);
        }

        private void ParseBody(List<byte> data)
        {
            var contentType = new string[0];
            var charsetValue = "";

            // Stop right here if the body is empty
            if (data.Count == 0)
            {
                return;
            }
            else
            {
                Body = new HttpBody();
            }

            // Get the content type to figure out how to parse the body
            if (Headers.Contains("content-type"))
            {
                contentType = Headers["content-type"].ToLower().Replace("\r\n", "").Replace(" ", "").Split(';');
                charsetValue = contentType.FirstOrDefault(x => x.StartsWith("charset"))?.Split('=')[1];
            }

            // Determine the body encoding based on the content type's value
            if (!string.IsNullOrEmpty(charsetValue))
            {
                CharSet = Encoding.GetEncoding(charsetValue);
            }

            // And than based on its value, parse the body
            if (contentType.Contains("multipart/form-data"))
            {
                var boundary = contentType[1].Replace("boundary=", "");
                boundary = boundary.Replace("\r", "");
                boundary = boundary.Replace("\n", "");

                ParseBodyMultipartFormData(data, boundary);
            }
            else if (contentType.Contains("application/x-www-form-urlencoded"))
            {
                ParseBodyUrlEncoded(data);
            }
            else
            {
                Body.Data = data.ToArray();
            }
        }

        private void ParseBodyUrlEncoded(List<byte> data)
        {
            var dataString = CharSet.GetString(data.ToArray());
            var keyValues = dataString.Split('&');

            if (Body.KeyValues == null)
            {
                Body.KeyValues = new Dictionary<string, string>();
            }

            foreach (var entry in keyValues)
            {
                var key = entry.Split('=')[0];
                var value = entry.Split('=')[1];
                var valueEncoded = HttpUtils.ConvertUrlEncoding(value);

                if (Body.KeyValues.ContainsKey(key))
                {
                    throw new HttpException(HttpStatusCode.BadRequest, $"Duplicate field {key} in multipart/form-data body");
                }

                Body.KeyValues.Add(key, valueEncoded);
            }
        }

        private void ParseBodyMultipartFormData(List<byte> data, string boundary)
        {
            var lineBytes = new List<byte>();
            var lineString = "";

            var contentType = "text/plain";
            var bodyBytes = new List<byte>();
            var fieldName = "";
            var fieldFilename = "";

            var closingBoundaryExists = false;

            // Parse the data
            foreach (var b in data)
            {
                var c = (char)b;

                // Add the char to the line
                lineString += c;

                // Add the byte to the list of bytes
                lineBytes.Add(b);

                // If the line ends, parse it
                if (!lineString.EndsWith("\r\n"))
                {
                    continue;
                }

                if (lineString.StartsWith("--" + boundary) && bodyBytes.Count == 0)
                {
                    // Just ignore as this marks the start of a new section or file
                }
                else if (lineString.StartsWith("--" + boundary) && bodyBytes.Count > 0)
                {
                    var charset = CharSet;
                    var charsetValue = contentType.Split(';').FirstOrDefault(x => x.StartsWith("charset"))?.Split('=')[1];

                    if (!string.IsNullOrEmpty(charsetValue))
                    {
                        charset = Encoding.GetEncoding(charsetValue);
                    }
                    
                    if (lineString == "--" + boundary + "--\r\n")
                    {
                        closingBoundaryExists = true;
                    }

                    if (!string.IsNullOrEmpty(fieldFilename))
                    {
                        var file = new HttpFile();
                        file.ContentType = contentType;
                        file.CharSet = charset;
                        file.Key = fieldName;
                        file.FileName = fieldFilename;
                        file.Data = bodyBytes.ToArray();

                        if (Body.Files == null)
                        {
                            Body.Files = new List<HttpFile>();
                        }

                        Body.Files.Add(file);
                    }
                    else
                    {
                        if (Body.KeyValues == null)
                        {
                            Body.KeyValues = new Dictionary<string, string>();
                        }

                        if (contentType.StartsWith("application/x-www-form-urlencoded"))
                        {
                            var contentString = charset.GetString(bodyBytes.ToArray());
                            var keyValues = contentString.Split('&');

                            foreach (var entry in keyValues)
                            {
                                var key = entry.Split('=')[0];
                                var value = entry.Split('=')[1];
                                var valueEncoded = HttpUtils.ConvertUrlEncoding(value);

                                if (Body.KeyValues.ContainsKey(key))
                                {
                                    throw new HttpException(HttpStatusCode.BadRequest, $"Duplicate field {key} in multipart/form-data body");
                                }

                                Body.KeyValues.Add(key, valueEncoded);
                            }
                        }
                        else if (contentType.StartsWith("text/plain"))
                        {
                            var value = CharSet.GetString(bodyBytes.ToArray());
                            value = value.Replace("\r", "");
                            value = value.Replace("\n", "");

                            Body.KeyValues.Add(fieldName, value);
                        }
                        else
                        {
                            throw new HttpException(HttpStatusCode.UnsupportedMediaType, $"Content-Type {contentType} not supported in multipart/form-data body");
                        }
                    }

                    contentType = "text/plain";
                    fieldName = "";
                    fieldFilename = "";
                    bodyBytes.Clear();
                }
                else if (lineString.ToLower().StartsWith("content-disposition"))
                {
                    // IETF RFC7578 4.2
                    //
                    // We just substring the length of the string "content-disposition: form-data" and trim it to remove all whitespace
                    // Then we split the remaining string (which looks like "name=user; filename=john.doe")
                    // We need not to worry about the value of content-disposition, the protocol requires it to be "form-data"
                    // If not the parsing will fail and an error occurs
                    var values = lineString.Replace("\r", "").Replace("\n", "").Split(';');

                    // Than loop through the values
                    for (var i=0; i<values.Length; i++)
                    {
                        var value = values[i];

                        // Most clients add spaces between the values so we need to trim those away
                        if (value.StartsWith(" "))
                        {
                            value = value.Substring(1);
                        }

                        if (value.ToLower().StartsWith("content-disposition"))
                        {
                            var contentDisposition = value.Substring("content-disposition: ".Length);

                            if (contentDisposition != "form-data")
                            {
                                // IETF RFC7578 4.2
                                throw new HttpException(HttpStatusCode.BadRequest, "Value of content-disposition in multipart/form-data body is required to be form-data");
                            }
                        }
                        if (value.ToLower().StartsWith("name"))
                        {
                            fieldName = value.Split('=')[1].Replace("\"", "");
                        }
                        if (value.ToLower().StartsWith("filename"))
                        {
                            fieldFilename = value.Split('=')[1].Replace("\"", "");
                        }
                    }
                }
                else if (lineString.ToLower().StartsWith("content-type"))
                {
                    contentType = lineString.Split(':')[1].Replace(" ", "").ToLower();
                }
                else if (lineString == "\r\n")
                {
                    // Ignore this as it marks the start of the body
                    // But we don't need that check here since we already passed all the others
                }
                else
                {
                    bodyBytes.AddRange(lineBytes);
                }

                lineString = "";
                lineBytes.Clear();
            }

            if (!closingBoundaryExists)
            {
                throw new HttpException(HttpStatusCode.BadRequest, "No closing boundary found in request body");
            }
        }

        private void FixHeaders()
        {
            // IETF RFC2045 5.2
            if (Method != HttpMethod.Get && Method != HttpMethod.Options && Headers.ContentType == null)
            {
                Headers.ContentType = "text/plain";
            }
        }

        public byte[] GenerateRaw()
        {
            var result = new List<byte>();
            var body = new List<byte>();
            var crlf = Encoding.ASCII.GetBytes("\r\n");
            var hyphens = Encoding.ASCII.GetBytes("--");

            // If the request contains files, we need to update some stuff as the request now becomes a multipart/formdata
            if (Body != null)
            {
                if (Body.Files != null)
                {
                    var boundaryString = "{[{__<>(^.^)<>___<>(-.-)<>___<>(>.<)<>___<>(~.~)<>__}]}";
                    var boundaryBytes = CharSet.GetBytes(boundaryString);

                    // Add our keyvalues first if any
                    if (Body.KeyValues != null)
                    {
                        foreach (var entry in Body.KeyValues)
                        {
                            var contentDisposition = $"content-disposition: form-data; name=\"{entry.Key}\"\r\n";  
                            var contentType = "content-type: text/plain\r\n";

                            body.AddRange(hyphens);
                            body.AddRange(boundaryBytes);
                            body.AddRange(crlf);
                            body.AddRange(CharSet.GetBytes(contentDisposition));
                            body.AddRange(CharSet.GetBytes(contentType));
                            body.AddRange(crlf);
                            body.AddRange(CharSet.GetBytes(entry.Value));
                        }
                    }

                    // Continue with all files
                    foreach (var file in Body.Files)
                    {
                        var contentDisposition = $"content-disposition: form-data; name=\"{file.Key}\"; filename=\"{file.FileName}\"\r\n";
                        var contentType = $"content-type: {file.ContentType}\r\n";

                        body.AddRange(hyphens);
                        body.AddRange(boundaryBytes);
                        body.AddRange(crlf);
                        body.AddRange(CharSet.GetBytes(contentDisposition));
                        body.AddRange(CharSet.GetBytes(contentType));
                        body.AddRange(crlf);
                        body.AddRange(file.Data);
                    }

                    // Add the final boundary
                    if (Body.KeyValues?.Count > 0 || Body.Files.Count > 0)
                    {
                        body.AddRange(hyphens);
                        body.AddRange(boundaryBytes);
                        body.AddRange(hyphens);
                        body.AddRange(crlf);
                    }

                    Headers.ContentType = $"multipart/form-data, boundary={boundaryString}";
                }
                else if (Body.KeyValues != null)
                {
                    var i = 0;

                    foreach (var entry in Body.KeyValues)
                    {
                        var keyValue = $"{entry.Key}={entry.Value}";
                        var bytes = CharSet.GetBytes(keyValue);

                        body.AddRange(bytes);
                        i++;

                        if (i < Body.KeyValues.Count)
                        {
                            body.Add((byte)'&');
                        }
                    }

                    Headers.ContentType = "application/x-www-form-urlencoded";
                }
                else if (Body.Data != null)
                {
                    body.AddRange(Body.Data);
                }

                Headers.ContentLength = body.Count;
            }

            result.AddRange(Encoding.ASCII.GetBytes($"{Method.ToString().ToUpper()} {Path}{Query} HTTP/1.1\r\n"));
            result.AddRange(Encoding.ASCII.GetBytes(Headers.ToString()));
            result.AddRange(crlf);
            result.AddRange(body);

            return result.ToArray();
        }
    }
}