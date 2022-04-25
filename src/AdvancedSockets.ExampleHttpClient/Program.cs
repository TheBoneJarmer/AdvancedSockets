using System;
using System.Text;
using AdvancedSockets.Http.Client;

namespace AdvancedSockets.ExampleHttpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new HttpClient();
            var uri = new Uri("https://www.rubenlabruyere.be");
            var request = new HttpClientRequest(uri);

            var response = client.Send(request);

            if (response != null)
            {
                Console.WriteLine((int)response.StatusCode);
                Console.WriteLine(response.Headers.ToString());
                Console.WriteLine(Encoding.UTF8.GetString(response.Body));
            }
            else
            {
                Console.WriteLine("No response received");
            }
        }
    }
}
