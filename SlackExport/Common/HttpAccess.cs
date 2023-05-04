using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SlackExport.Common
{
    public class HttpAccess
    {
        public string get(string url, string token)
        {
            string responseBody = string.Empty;

            string bearerToken = "Bearer " + " " + token;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", bearerToken);
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    responseBody = System.Text.RegularExpressions.Regex.Unescape(responseBody);
                }
            }
            return responseBody;
        }
    }
}

