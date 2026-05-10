using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace project_docs_summariser
{
    public static class AiService
    {
        private const string ApiEndpoint = "https://api.groq.com/openai/v1/chat/completions";
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<string> GetResponseAsync(string message)
        {
            // Fully qualified path guarantees the compiler finds your newly generated setting
            string apiKey = project_docs_summariser.Properties.Settings.Default.ApiKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("API Key is missing. Please save your API key in the dashboard before continuing.");
            }

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "user", content = message }
                }
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint))
            {
                requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
                requestMessage.Content = content;

                var response = await httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;
                    return root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                }
            }
        }
    }
}