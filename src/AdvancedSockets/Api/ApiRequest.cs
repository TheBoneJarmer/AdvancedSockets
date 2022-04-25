using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AdvancedSockets.Http;
using AdvancedSockets.Http.Client;
using Newtonsoft.Json;

namespace AdvancedSockets.Api
{
    public class ApiRequest
    {
        private string url;
        private string authorization;

        public ApiRequest(string url, string authorization = null)
        {
            this.url = url;
            this.authorization = authorization;
        }

        public ApiResponse Get(string path)
        {
            return Send(HttpMethod.Get, path);
        }
        public ApiResponse Post(string path, object body = null)
        {
            return Send(HttpMethod.Post, path, body);
        }
        public ApiResponse Put(string path, object body = null)
        {
            return Send(HttpMethod.Put, path, body);
        }
        public ApiResponse Delete(string path, object body = null)
        {
            return Send(HttpMethod.Delete, path, body);
        }

        private ApiResponse Send(HttpMethod method, string path, object body = null)
        {
            try
            {
                var uri = new Uri(url + path);
                var client = new HttpClient();
                
                var httpRequest = new HttpClientRequest(uri);
                httpRequest.Method = method;
                httpRequest.Headers.Authorization = authorization;

                if (body != null)
                {
                    httpRequest.Headers.ContentType = "application/json; charset=UTF-8";
                }

                if (body != null)
                {
                    var jsonString = JsonConvert.SerializeObject(body);
                    var jsonBytes = Encoding.UTF8.GetBytes(jsonString);

                    httpRequest.Body = new HttpBody();
                    httpRequest.Body.Data = jsonBytes;
                }

                var httpResponse = client.Send(httpRequest);
                var bodyString = Encoding.UTF8.GetString(httpResponse.Body);
                var apiResponse = new ApiResponse(httpResponse.StatusCode, bodyString);

                return apiResponse;
            }
            catch (SocketException)
            {
                return new ApiResponse(HttpStatusCode.ServiceUnavailable, "Unable to connect to server");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
