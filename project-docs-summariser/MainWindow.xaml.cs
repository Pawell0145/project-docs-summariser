using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace WpfAiIntegration
{
    public partial class MainWindow : Window
    {
        private const string ApiKey = "api key";
        private const string ApiEndpoint = "https://api.groq.com/openai/v1/chat/completions";
        private static readonly HttpClient httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userMessage = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(userMessage)) return;

            SendButton.IsEnabled = false;
            OutputTextBox.Text = "Processing...";

            try
            {
                string aiResponse = await GetAiResponseAsync(userMessage);
                OutputTextBox.Text = aiResponse;
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Error: {ex.Message}";
            }
            finally
            {
                SendButton.IsEnabled = true;
            }
        }

        private async Task<string> GetAiResponseAsync(string message)
        {
            // 1. data request - depends on api
            var requestBody = new
            {
                model = "llama-3.3-70b-versatile", // model name
                messages = new[]
                {
                    new { role = "user", content = message }
                }
            };

            // 2. change to JSON
            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 3. authentication
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint))
            {
                requestMessage.Headers.Add("Authorization", $"Bearer {ApiKey}");
                requestMessage.Content = content;

                // 4. async send request
                var response = await httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                // 5. receive and decode response
                string responseBody = await response.Content.ReadAsStringAsync();

                // 6. Extract the text from the JSON
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;
                    // The path to the text depends on the API provider! (Here is the OpenAI structure)
                    string textResult = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    return textResult;
                }
            }
        }
    }
}