using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using AdvancedSockets.Http;
using AdvancedSockets.Http.Server;

namespace AdvancedSockets.ExampleHttpServer
{
    class Program
    {
        private static HttpServer server;

        static void Main(string[] args)
        {
            server = new HttpServer("localhost", 8080);
            server.OnRequest += Server_OnRequest;
            server.OnException += Server_OnException;
            server.OnHttpError += Server_OnHttpError;
            server.Start();
        }

        private static void Server_OnHttpError(HttpStatusCode status, string error, HttpRequest request, HttpResponse response, HttpConnectionInfo info)
        {
            var html = "";
            html += "<!DOCTYPE html>";
            html += "<html lang='en'>";
            html += "<head>";
            html += "<title>Error</title>";
            html += "<meta charset='utf-8' />";
            html += "</head>";
            html += "<body>";
            html += "<h1>Uh oh..</h1>";
            html += $"<p>{error}</p>";
            html += "</body>";
            html += "</htmL>";

            response.StatusCode = status;
            response.Headers.ContentType = "text/html";
            response.Body = Encoding.ASCII.GetBytes(html);
            response.Send();
        }

        private static void Server_OnException(Exception ex)
        {
            Console.WriteLine("An error occured");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        private static void Server_OnRequest(HttpRequest request, HttpResponse response, HttpConnectionInfo info)
        {
            Console.WriteLine($"{request.Method.ToString().ToUpper()} {request.Path}");
                        
            var body = "";
            var status = HttpStatusCode.OK;
            var contentType = "text/html";

            if (request.Method == HttpMethod.Get && request.Path == "/")
            {
                body += "<h1>Welcome!</h1>";
                body += "<p>This is an example of AdvancedSocket's http server capabilities. Click the links below to see the different features. Have fun!</p>";
                body += "<a href='/upload'>Upload Example</a><br>";
                body += "<a href='/files'>Uploaded Files</a>";
            }
            else if (request.Method == HttpMethod.Get && request.Path == "/upload")
            {
                body = $"<form method='post' action='/upload' enctype='multipart/form-data'><input type='file' name='file' multiple><input type='submit' value='upload'></form>";
            }
            else if (request.Method == HttpMethod.Get && request.Path == "/files")
            {
                if (!Directory.Exists("uploads"))
                {
                    body += "No files were uploaded yet";
                }
                else
                {
                    foreach (var file in Directory.GetFiles("uploads"))
                    {
                        var filename = file.Replace("uploads/", "");
                        body += $"<a href='/file?filename={filename}'>{filename}</a><br>";
                    }
                }
            }
            else if (request.Method == HttpMethod.Get && request.Path == "/file")
            {
                var filename = "";

                if (request.Query.Contains("filename"))
                {
                    filename = request.Query["filename"];
                }

                if (filename.Length == 0)
                {          
                    body += "<p>No filename provided in url</p>";
                }
                else
                {
                    response.SendFile($"uploads2/{filename}");
                    return;
                }
            }
            else if (request.Method == HttpMethod.Post && request.Path == "/upload")
            {
                if (!Directory.Exists("uploads"))
                {
                    Directory.CreateDirectory("uploads");
                }

                if (request.Body.Files == null)
                {
                    body = "<h1>How Embarrassing <>(>.>)<>..</h1><p>Something went wrong during file upload</p>";    
                }
                else
                {
                    foreach (var file in request.Body.Files)
                    {
                        Console.WriteLine($"Uploading file {file.FileName}");
                        File.WriteAllBytes($"uploads/{file.FileName}", file.Data.ToArray());
                    }

                    body = $"<h1>Success!</h1><p>All {request.Body.Files.Count} files have been uploaded to the folder uploads</p><a href='/'>Back</a>";
                }
            }
            else
            {
                status = HttpStatusCode.NotFound;
                body = $"Unknown endpoint {request.Method.ToString().ToUpper()} {request.Path}";
            }
            response.Headers.ContentType = contentType;
            response.StatusCode = status;
            response.Body = Encoding.ASCII.GetBytes($"<!DOCTYPE html><html><head><title>My HTTP server</title><meta charset='utf-8' /><body>{body}</body></html>");
            response.Send();
        }
    }
}
