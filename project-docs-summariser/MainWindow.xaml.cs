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
        private const string ApiEndpoint = "https://api.groq.com/openai/v1/chat/completions";
        private static readonly HttpClient httpClient = new HttpClient();
        private List<string> selectedFilePaths = new List<string>();

        // Architecture Paths
        private readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private readonly string HistoryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.json");

        // State
        private AppConfig _config = new AppConfig();
        private List<HistoryEntry> _history = new List<HistoryEntry>();

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            LoadHistory();
            UpdateFileListUI();
        }

        private void LoadConfig()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    ApplyTheme(_config.IsDarkMode);
                }
                catch { /* Use defaults if corrupted */ }
            }
        }

        private void SaveConfig()
        {
            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(_config));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ApiKeyTextBox.Text = _config.ApiKey;
            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _config.ApiKey = ApiKeyTextBox.Text.Trim();
            SaveConfig();
            SettingsOverlay.Visibility = Visibility.Collapsed;
            UpdateStatus("Settings saved.");
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _config.IsDarkMode = !_config.IsDarkMode;
            ApplyTheme(_config.IsDarkMode);
            SaveConfig();
        }

        private void ApplyTheme(bool isDark)
        {
            var res = Application.Current.MainWindow.Resources;
            var converter = new BrushConverter();

            if (isDark)
            {
                res["AppBackgroundBrush"] = (Brush)converter.ConvertFromString("#111827");
                res["CardBackgroundBrush"] = (Brush)converter.ConvertFromString("#1F2937");
                res["TextPrimaryBrush"] = (Brush)converter.ConvertFromString("#F9FAFB");
                res["TextSecondaryBrush"] = (Brush)converter.ConvertFromString("#9CA3AF");
                res["BorderBrush"] = (Brush)converter.ConvertFromString("#374151");
                res["SecondaryActionBrush"] = (Brush)converter.ConvertFromString("#374151");
            }
            else
            {
                res["AppBackgroundBrush"] = (Brush)converter.ConvertFromString("#F3F4F6");
                res["CardBackgroundBrush"] = (Brush)converter.ConvertFromString("White");
                res["TextPrimaryBrush"] = (Brush)converter.ConvertFromString("#111827");
                res["TextSecondaryBrush"] = (Brush)converter.ConvertFromString("#6B7280");
                res["BorderBrush"] = (Brush)converter.ConvertFromString("#E5E7EB");
                res["SecondaryActionBrush"] = (Brush)converter.ConvertFromString("#E5E7EB");
            }
        }

        private void LoadHistory()
        {
            if (File.Exists(HistoryFilePath))
            {
                try
                {
                    string json = File.ReadAllText(HistoryFilePath);
                    _history = JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new List<HistoryEntry>();
                }
                catch { _history = new List<HistoryEntry>(); }
            }
        }

        private void SaveHistoryEntry(string summary, string fileNames)
        {
            _history.Insert(0, new HistoryEntry
            {
                Timestamp = DateTime.Now,
                FullSummary = summary,
                FileNames = fileNames
            });

            // Keep only the last 50 to prevent huge files
            if (_history.Count > 50) _history = _history.Take(50).ToList();

            File.WriteAllText(HistoryFilePath, JsonSerializer.Serialize(_history));
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryListBox.ItemsSource = null;
            HistoryListBox.ItemsSource = _history;
            HistoryOverlay.Visibility = Visibility.Visible;
        }

        private void CloseHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryOverlay.Visibility = Visibility.Collapsed;
        }

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryListBox.SelectedItem is HistoryEntry selectedEntry)
            {
                OutputTextBox.Text = selectedEntry.FullSummary;
                SetOutputActionsVisibility(Visibility.Visible);
                UpdateStatus($"Loaded past summary from {selectedEntry.DateString}");
                HistoryOverlay.Visibility = Visibility.Collapsed;
                HistoryListBox.SelectedItem = null;
            }
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Text Files (*.txt)|*.txt", Multiselect = true };
            if (dialog.ShowDialog() == true) AddFilesToList(dialog.FileNames);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddFilesToList(files.Where(f => Path.GetExtension(f).ToLower() == ".txt").ToArray());
            }
        }

        private void AddFilesToList(string[] filePaths)
        {
            foreach (var path in filePaths)
            {
                if (!selectedFilePaths.Contains(path)) selectedFilePaths.Add(path);
            }
            UpdateFileListUI();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFilePaths.Clear();
            UpdateFileListUI();
            OutputTextBox.Text = "Your summary will appear here...";
            SetOutputActionsVisibility(Visibility.Collapsed);
            UpdateStatus("Ready");
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is FileItem item)
            {
                selectedFilePaths.Remove(item.FullPath);
                UpdateFileListUI();
            }
        }

        private void UpdateFileListUI()
        {
            FileListBox.ItemsSource = selectedFilePaths.Select(p => new FileItem { FullPath = p, FileName = Path.GetFileName(p) }).ToList();
            bool hasFiles = selectedFilePaths.Count > 0;
            PlaceholderVisual.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
            SendButton.IsEnabled = hasFiles;
            UpdateStatus(hasFiles ? $"{selectedFilePaths.Count} files selected" : "Ready");
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                SettingsOverlay.Visibility = Visibility.Visible;
                UpdateStatus("API Key required.");
                return;
            }

            if (selectedFilePaths.Count == 0) return;

            SendButton.IsEnabled = false;
            LoadingProgress.Visibility = Visibility.Visible;
            OutputTextBox.Text = "Reading files and contacting AI model...";
            UpdateStatus("Processing...");

            try
            {
                string combinedContent = CombineFileContents();
                string prompt = "Provide a comprehensive summary of these documents using bullet points:\n\n" + combinedContent;

                string aiResponse = await GetAiResponseAsync(prompt);
                OutputTextBox.Text = aiResponse;

                string fileNames = string.Join(", ", selectedFilePaths.Select(Path.GetFileName));
                SaveHistoryEntry(aiResponse, fileNames);

                SetOutputActionsVisibility(Visibility.Visible);
                UpdateStatus("Summary generated.");
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Error: {ex.Message}";
                UpdateStatus("Error occurred.");
            }
            finally
            {
                SendButton.IsEnabled = true;
                LoadingProgress.Visibility = Visibility.Collapsed;
            }
        }

        private string CombineFileContents()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var path in selectedFilePaths)
            {
                sb.AppendLine($"### {Path.GetFileName(path)} ###");
                sb.AppendLine(File.ReadAllText(path));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private async Task<string> GetAiResponseAsync(string message)
        {
            var requestBody = new { model = "llama-3.3-70b-versatile", messages = new[] { new { role = "user", content = message } }, temperature = 0.5 };
            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint))
            {
                requestMessage.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
                requestMessage.Content = content;

                var response = await httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
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
            Clipboard.SetText(OutputTextBox.Text);
            UpdateStatus("Copied to clipboard.");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "Text File|*.txt", FileName = $"Summary_{DateTime.Now:yyyyMMdd_HHmm}.txt" };
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, OutputTextBox.Text);
                UpdateStatus("Saved to file.");
            }
        }

        private void UpdateStatus(string text) => StatusTextBlock.Text = text;
    }

    public class FileItem
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
    }

    public class AppConfig
    {
        public string ApiKey { get; set; } = "";
        public bool IsDarkMode { get; set; } = false;
    }

    public class HistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string FileNames { get; set; }
        public string FullSummary { get; set; }

        public string DateString => Timestamp.ToString("MMM dd, yyyy - HH:mm");
        public string Excerpt => FullSummary.Length > 150 ? FullSummary.Substring(0, 150) + "..." : FullSummary;
    }
}