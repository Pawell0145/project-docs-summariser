using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            UpdateFileListUI();
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
                AddFilesToList(openFileDialog.FileNames);
            }
        }

        // Handles Drag & Drop over the window
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                FileListBoxBorder.BorderBrush = (SolidColorBrush)FindResource("PrimaryActionBrush");
                FileListBoxBorder.Background = new SolidColorBrush(Color.FromArgb(10, 79, 70, 229));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        // Handles dropping files onto the window
        private void Window_Drop(object sender, DragEventArgs e)
        {
            ResetDropVisuals();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var textFiles = files.Where(f => Path.GetExtension(f).Equals(".txt", StringComparison.OrdinalIgnoreCase)).ToArray();

                if (textFiles.Length < files.Length)
                {
                    UpdateStatus("Some non-text files were ignored.", "#F59E0B");
                }

                AddFilesToList(textFiles);
            }
        }

        private void ResetDropVisuals()
        {
            FileListBoxBorder.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");
            FileListBoxBorder.Background = Brushes.White;
        }

        private void AddFilesToList(string[] filePaths)
        {
            foreach (var path in filePaths)
            {
                // Prevent duplicates
                if (!selectedFilePaths.Contains(path))
                {
                    selectedFilePaths.Add(path);
                }
            }
            UpdateFileListUI();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFilePaths.Clear();
            UpdateFileListUI();
            OutputTextBox.Text = "Your summary will appear here after clicking 'Generate Summary'...";
            SetOutputActionsVisibility(Visibility.Collapsed);
            UpdateStatus("Ready");
        }

        // Handles individual file removal via the "X" button
        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string filePathToRemove = clickedButton.DataContext as string;

            if (filePathToRemove != null)
            {
                selectedFilePaths.Remove(filePathToRemove);
                UpdateFileListUI();
            }
        }

        private void UpdateFileListUI()
        {
            FileListBox.ItemsSource = null;
            FileListBox.ItemsSource = selectedFilePaths.Select(p => Path.GetFileName(p)).ToList();

            bool hasFiles = selectedFilePaths.Count > 0;
            PlaceholderVisual.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
            SendButton.IsEnabled = hasFiles;

            UpdateStatus(hasFiles ? $"{selectedFilePaths.Count} files selected" : "Ready");
        }



        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFilePaths.Count == 0) return;


            SendButton.IsEnabled = false;
            FileListBox.IsEnabled = false;
            SelectFileButton.IsEnabled = false;
            ClearButton.IsEnabled = false;
            OutputTextBox.Text = "Reading files and contacting AI model...";
            LoadingProgress.Visibility = Visibility.Visible;
            SetOutputActionsVisibility(Visibility.Collapsed);
            UpdateStatus("Processing...", "#4F46E5");

            try
            {
                string combinedContent = CombineFileContents();

                StringBuilder prompt = new StringBuilder();
                prompt.AppendLine("You are an expert document analyst. Please provide a clear, comprehensive summary of the following text documents.");
                prompt.AppendLine("Use bullet points for key takeaways and ensure the summary merges information from all files logically.");
                prompt.AppendLine("\n--- BEGIN DOCUMENTS ---\n");
                prompt.AppendLine(combinedContent);
                prompt.AppendLine("\n--- END DOCUMENTS ---");

                string aiResponse = await GetAiResponseAsync(prompt.ToString());
                OutputTextBox.Text = aiResponse;
                UpdateStatus("Summary generated successfully", "#10B981");
                SetOutputActionsVisibility(Visibility.Visible);
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Error generating summary.\n\nDetails: {ex.Message}";
                UpdateStatus("Error", "#EF4444");
            }
            finally
            {
                SendButton.IsEnabled = true;
                FileListBox.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
                LoadingProgress.Visibility = Visibility.Collapsed;
            }
        }

        private string CombineFileContents()
        {
            StringBuilder combinedContent = new StringBuilder();
            for (int i = 0; i < selectedFilePaths.Count; i++)
            {
                string fileName = Path.GetFileName(selectedFilePaths[i]);
                string fileContent = "Could not read file.";
                try
                {
                    fileContent = File.ReadAllText(selectedFilePaths[i]);
                }
                catch (Exception ex)
                {
                    fileContent = $"[Error reading file: {ex.Message}]";
                }

                combinedContent.AppendLine($"### DOCUMENT {i + 1}: {fileName} ###");
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
                },
                temperature = 0.5
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

  

        private void SetOutputActionsVisibility(Visibility visibility)
        {
            CopyButton.Visibility = visibility;
            SaveButton.Visibility = visibility;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(OutputTextBox.Text))
            {
                Clipboard.SetText(OutputTextBox.Text);
                UpdateStatus("Summary copied to clipboard");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OutputTextBox.Text)) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text File (*.txt)|*.txt",
                DefaultExt = "txt",
                FileName = $"Summary_{DateTime.Now:yyyyMMdd_HHmm}.txt",
                Title = "Save Summary As"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, OutputTextBox.Text);
                UpdateStatus($"Summary saved to {Path.GetFileName(saveFileDialog.FileName)}");
            }
        }

        
        private void UpdateStatus(string text, string hexColor = "#6B7280")
        {
            StatusTextBlock.Text = text;
            try
            {
                StatusDot.Fill = (SolidColorBrush)new CornerRadiusConverter().ConvertFromString(hexColor);
                StatusDot.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor);
            }
            catch {}
        }

    }
}