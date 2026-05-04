using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace WpfAiIntegration
{
    public partial class MainWindow : Window
    {
        private const string ApiKey = "api_key";
        private const string ApiEndpoint = "https://api.groq.com/openai/v1/chat/completions";
        private static readonly HttpClient httpClient = new HttpClient();
        private List<string> selectedFilePaths = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Select text files",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePaths.AddRange(openFileDialog.FileNames);
                UpdateFileList();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFilePaths.Clear();
            UpdateFileList();
        }

        private void UpdateFileList()
        {
            FileListBox.Items.Clear();
            foreach (var filePath in selectedFilePaths)
            {
                FileListBox.Items.Add(Path.GetFileName(filePath));
            }
            FileCountTextBlock.Text = $"Selected files: {selectedFilePaths.Count}";
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFilePaths.Count == 0)
            {
                OutputTextBox.Text = "Please select at least one file.";
                return;
            }

            SendButton.IsEnabled = false;
            OutputTextBox.Text = "Processing...";

            try
            {
                string combinedContent = CombineFileContents();
                string aiResponse = await GetAiResponseAsync(combinedContent);
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

        private string CombineFileContents()
        {
            StringBuilder combinedContent = new StringBuilder();
            for (int i = 0; i < selectedFilePaths.Count; i++)
            {
                string fileName = Path.GetFileName(selectedFilePaths[i]);
                string fileContent = File.ReadAllText(selectedFilePaths[i]);
                
                combinedContent.AppendLine($"=== File: {fileName} ===");
                combinedContent.AppendLine(fileContent);
                
                if (i < selectedFilePaths.Count - 1)
                {
                    combinedContent.AppendLine();
                }
            }
            return combinedContent.ToString();
        }

        private async Task<string> GetAiResponseAsync(string message)
        {
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
                requestMessage.Headers.Add("Authorization", $"Bearer {ApiKey}");
                requestMessage.Content = content;

                var response = await httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;
                    string textResult = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    return textResult;
                }
            }
        }
    }
}