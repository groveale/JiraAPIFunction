using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace groverale.Function
{
    public static class JiraHelpers
    {
        public static string GetAuthHeaderValue()
        {
            string userName = Environment.GetEnvironmentVariable("JiraAdminUsername");
            string apiToken = Environment.GetEnvironmentVariable("JiraAdminAPIToken");

            string login = $"{userName}:{apiToken}";

            return EncodeTo64(login);
        }

        public static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes = Encoding.ASCII.GetBytes(toEncode);
            return Convert.ToBase64String(toEncodeAsBytes);
        }

        public static HttpClient InitHTTPClient()
        {
            string jiraCloudURI = Environment.GetEnvironmentVariable("JiraCloudBaseUri");

            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(jiraCloudURI)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Add auth
            client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Basic", GetAuthHeaderValue());

            return client;
        }

        public static async Task<dynamic> ReadResposneData (HttpResponseMessage responseMessage)
        {
            string responseBody = String.Empty;
            using (StreamReader streamReader =  new  StreamReader(await responseMessage.Content.ReadAsStreamAsync()))
            {
                responseBody = await streamReader.ReadToEndAsync();
            }
            
            return JsonConvert.DeserializeObject(responseBody);
        }
    
        public static bool ContainsKey(dynamic newtonsoftDynamic, string propertyName) {
            return (newtonsoftDynamic as JObject).ContainsKey(propertyName);
        }
    }
}