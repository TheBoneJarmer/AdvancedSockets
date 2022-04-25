using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AdvancedSockets.Http
{
    public class HttpBody
    {
        public byte[] Data { get; set; }
        public Dictionary<string, string> KeyValues { get; set; }
        public List<HttpFile> Files { get; set; }

        internal HttpBody()
        {
            
        }
        public HttpBody(string data)
        {
            this.Data = Encoding.ASCII.GetBytes(data);
        }
        public HttpBody(byte[] data)
        {
            this.Data = data;
        }
        public HttpBody(Dictionary<string, string> keyValues)
        {
            this.KeyValues = keyValues;
        }
        public HttpBody(List<HttpFile> files)
        {
            this.Files = files;
        }
        public HttpBody(HttpFile[] files)
        {
            this.Files = files.ToList();
        }
    }
}
