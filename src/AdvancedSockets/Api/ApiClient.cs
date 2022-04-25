using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdvancedSockets.Api
{
    public class ApiClient
    {
        private string url;

        public ApiClient(string url)
        {
            this.url = url;
        }

        public string Get(string path, string authorization)
        {
            ApiRequest request = new ApiRequest(url, authorization);
            ApiResponse response = request.Get(path);
            response.ThrowExceptionWhenBadStatus();
            return response.Body;
        }
        public T Get<T>(string path, string authorization)
        {
            return JsonConvert.DeserializeObject<T>(Get(path, authorization));
        }

        public string Post(string path, string authorization, object body = null)
        {
            ApiRequest request = new ApiRequest(url, authorization);
            ApiResponse response = request.Post(path, body);
            response.ThrowExceptionWhenBadStatus();
            return response.Body;
        }
        public T Post<T>(string path, string authorization, object body = null)
        {
            return JsonConvert.DeserializeObject<T>(Post(path, authorization, body));
        }

        public string Put(string path, string authorization, object body = null)
        {
            ApiRequest request = new ApiRequest(url, authorization);
            ApiResponse response = request.Put(path, body);
            response.ThrowExceptionWhenBadStatus();
            return response.Body;
        }
        public T Put<T>(string path, string authorization, object body = null)
        {
            return JsonConvert.DeserializeObject<T>(Put(path, authorization, body));
        }

        public string Delete(string path, string authorization, object body = null)
        {
            ApiRequest request = new ApiRequest(url, authorization);
            ApiResponse response = request.Delete(path, body);
            response.ThrowExceptionWhenBadStatus();
            return response.Body;
        }
        public T Delete<T>(string path, string authorization, object body = null)
        {
            return JsonConvert.DeserializeObject<T>(Delete(path, authorization, body));
        }
    }
}
