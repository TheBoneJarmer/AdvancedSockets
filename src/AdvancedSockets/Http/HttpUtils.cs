using System;
using System.Net;

namespace AdvancedSockets.Http
{
    public static class HttpUtils
    {
        public static string GetStatusText(HttpStatusCode statusCode)
        {
            return GetStatusText((int)statusCode);
        }
        public static string GetStatusText(int statusCode)
        {
            switch(statusCode)
            {
                case 200:
                    return "OK";
                case 301:
                    return "Redirect";
                case 400:
                    return "Bad request";
                case 401:
                    return "Unauthorized";
                case 403:
                    return "Forbidden";
                case 404:
                    return "Not found";
                case 415:
                    return "Unsupported Media Type";
                case 500:
                    return "Internal Server Error";
            }

            return "Undefined";
        }
        public static string GetContentType(string fileName)
        {           
            if (fileName.EndsWith(".html")) return "text/html";
            if (fileName.EndsWith(".htm")) return "text/html";
            if (fileName.EndsWith(".css")) return "text/css";
            if (fileName.EndsWith(".js")) return "application/javascript";
            if (fileName.EndsWith(".json")) return "application/json";
            if (fileName.EndsWith(".xml")) return "application/xml";

            if (fileName.EndsWith(".bmp")) return "image/bmp";
            if (fileName.EndsWith(".jpg")) return "image/jpeg";
            if (fileName.EndsWith(".jpeg")) return "image/jpeg";
            if (fileName.EndsWith(".png")) return "image/png";
            if (fileName.EndsWith(".gif")) return "image/gif";
            if (fileName.EndsWith(".ico")) return "image/x-icon";

            if (fileName.EndsWith(".otf")) return "font/otf";
            if (fileName.EndsWith(".ttf")) return "font/ttf";

            if (fileName.EndsWith(".wav")) return "audio/wav";
            if (fileName.EndsWith(".ogg")) return "audio/ogg";
            if (fileName.EndsWith(".mp3")) return "audio/mp3";

            if (fileName.EndsWith(".pdf")) return "application/pdf";
            if (fileName.EndsWith(".rar")) return "application/x-rar-compressed";
            if (fileName.EndsWith(".zip")) return "application/zip";
            if (fileName.EndsWith(".7z")) return "application/x-7z-compressed";
            if (fileName.EndsWith(".tar")) return "application/x-tar";

            return "text/plain";
        }
        public static string ConvertUrlEncoding(string input)
        {
            string output = input;

            for (int i=0; i<255; i++)
            {
                string hex = "%" + i.ToString("X").PadLeft(2, '0');
                string chr = ((char)i).ToString();

                output = output.Replace(hex, chr);
            }

            output = output.Replace("+", " ");

            return output;
        }
    }
}