using System;
using System.Collections.Generic;
using System.Text;

namespace AdvancedSockets.Http
{
    public class HttpFile
    {
        public string Key { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public Encoding CharSet { get; set; }

        internal HttpFile()
        {
            
        }
    }
}
