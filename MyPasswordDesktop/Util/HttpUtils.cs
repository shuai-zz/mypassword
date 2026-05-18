using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Thin synchronous HTTP helpers — the C# equivalent of the Java
    /// <c>HttpUtils</c> built on <c>java.net.http.HttpClient</c>.
    /// </summary>
    public static class HttpUtils
    {
        private static readonly HttpClient Client = new();

        public static string Get(string url, IDictionary<string, string> query,
            IDictionary<string, string> headers)
        {
            url = AppendQuery(url, query);
            Log.Info("GET: " + url);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            BindHeaders(request, headers);
            using var response = Client.Send(request);
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        public static string AppendQuery(string url, IDictionary<string, string> query)
        {
            if (query != null)
            {
                string q = ToQueryString(query);
                int pos = url.IndexOf('?');
                url = url + (pos < 0 ? "?" : "&") + q;
            }
            return url;
        }

        public static string PostForm(string url, IDictionary<string, string> query,
            IDictionary<string, string> headers)
        {
            string body = ToQueryString(query);
            Log.Info($"POST: {url}, form data: {body}");
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"),
            };
            BindHeaders(request, headers);
            using var response = Client.Send(request);
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        private static void BindHeaders(HttpRequestMessage request, IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                return;
            }
            foreach (var kv in headers)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }
        }

        public static string ToQueryString(IDictionary<string, string> query)
        {
            var sb = new StringBuilder();
            foreach (var kv in query)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    sb.Append(kv.Key).Append('=').Append(Uri.EscapeDataString(kv.Value)).Append('&');
                }
            }
            if (sb.Length > 0)
            {
                sb.Length--; // drop trailing '&'
            }
            return sb.ToString();
        }
    }
}
