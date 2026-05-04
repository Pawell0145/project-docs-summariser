using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfAiIntegration
{
    public partial class CreatePlanWindow : Window
    {
        private const string ApiKey = "apikey";
        private const string ApiEndpoint = "https://api.groq.com/openai/v1/chat/completions";
        private static readonly HttpClient httpClient = new HttpClient();
        public List<string> SelectedFiles { get; private set; } = new List<string>();

        public string GeneratedPlan { get; private set; }

        public CreatePlanWindow()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Select study materials",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    if (!SelectedFiles.Contains(file))
                    {
                        SelectedFiles.Add(file);
                        FilesListBox.Items.Add(Path.GetFileName(file));
                    }
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private async void CreatePlan_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TopicTextBox.Text) ||
                string.IsNullOrWhiteSpace(DaysTextBox.Text) ||
                string.IsNullOrWhiteSpace(HoursTextBox.Text))
            {
                MessageBox.Show("Please fill in Topic, Days, and Hours.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Button createBtn = (Button)sender;
            createBtn.IsEnabled = false;
            createBtn.Content = "GENERATING...";
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                string topic = TopicTextBox.Text;
                string days = DaysTextBox.Text;
                string hours = HoursTextBox.Text;
                string notes = NotesTextBox.Text;

                string prompt = $"You are an educational assistant. The user wants to study: {topic}. " +
                                $"They have {days} days to prepare, dedicating {hours} hours per day. " +
                                $"Their preferences/notes are: {notes}. " +
                                $"Generate a detailed, day-by-day study schedule. Ensure consistency.\n\n" +
                                $"CRITICAL INSTRUCTION: You MUST begin the section for each day exactly with the marker '|||DAY X|||' (where X is the day number). " +
                                $"Do not add any introductory text before the first day marker! Your response must start with '|||DAY 1|||'.";

                if (SelectedFiles.Count > 0)
                {
                    string combinedContent = CombineFileContents();
                    prompt += $"\n\nBase your schedule STRICTLY on the following materials provided by the user:\n{combinedContent}";
                }
                else
                {
                    prompt += $"\n\nThe user did not provide any specific materials. Base the schedule on your general knowledge of the topic.";
                }

                GeneratedPlan = await GetAiResponseAsync(prompt);

                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:\n{ex.Message}", "API Error", MessageBoxButton.OK, MessageBoxImage.Error);
                GeneratedPlan = null;
            }
            finally
            {
                Mouse.OverrideCursor = null;
                createBtn.IsEnabled = true;
                createBtn.Content = "CREATE";
            }
        }

        private string CombineFileContents()
        {
            StringBuilder combinedContent = new StringBuilder();
            for (int i = 0; i < SelectedFiles.Count; i++)
            {
                string fileName = Path.GetFileName(SelectedFiles[i]);
                string fileContent = File.ReadAllText(SelectedFiles[i]); 

                combinedContent.AppendLine($"=== File: {fileName} ===");
                combinedContent.AppendLine(fileContent);
                combinedContent.AppendLine();
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
                    return root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                }
            }
        }
    }
}