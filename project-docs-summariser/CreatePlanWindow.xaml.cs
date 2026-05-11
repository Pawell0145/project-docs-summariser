using project_docs_summariser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace WpfAiIntegration
{
    public partial class CreatePlanWindow : Window
    {
        private Dictionary<string, string> extractedDocuments = new Dictionary<string, string>();

        public CreatePlanWindow()
        {
            InitializeComponent();
        }

        public string GeneratedPlan { get; private set; }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Source Documents",
                Filter = "Supported Documents|*.pdf;*.docx;*.pptx|PDF Files|*.pdf|Word Documents (*.docx)|*.docx|Presentations (*.pptx)|*.pptx",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        if (extractedDocuments.ContainsKey(fileName)) continue;

                        string extractedText = DocumentExtractor.ExtractText(filePath);

                        if (!string.IsNullOrWhiteSpace(extractedText))
                        {
                            extractedDocuments[fileName] = extractedText;
                            FilesListBox.Items.Add(fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to process {Path.GetFileName(filePath)}: {ex.Message}", "Extraction Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void FilesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && FilesListBox.SelectedItem != null)
            {
                string selectedFile = FilesListBox.SelectedItem.ToString();
                extractedDocuments.Remove(selectedFile);
                FilesListBox.Items.Remove(FilesListBox.SelectedItem);
            }
        }

        private async void CreatePlan_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TopicTextBox.Text))
            {
                MessageBox.Show("Please enter a subject before creating a plan.", "Missing Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            MainContent.Opacity = 0.3;

            string subject = TopicTextBox.Text;
            string daysText = DaysTextBox.Text;
            string hoursText = HoursTextBox.Text;
            string notes = NotesTextBox.Text;

            int days = 0;
            int hours = 0;
            int.TryParse(daysText, out days);
            int.TryParse(hoursText, out hours);
            string finalSourceText = "";
            foreach (var doc in extractedDocuments)
            {
                finalSourceText += $"\n\n--- SOURCE DOCUMENT: {doc.Key} ---\n{doc.Value}";
            }

            string appLang = project_docs_summariser.Properties.Settings.Default.AppLanguage;
            if (string.IsNullOrEmpty(appLang)) appLang = "English";

            string fullPrompt = $@"You are an elite academic planner.
                    Create a highly structured SYLLABUS/OUTLINE for the subject: {subject}.
                    Total Timeframe: {days} days, studying {hours} hours per day.
                    Additional Preferences/Notes: {notes}

                    CRITICAL INSTRUCTIONS:
                    1. LANGUAGE: You MUST generate the ENTIRE syllabus strictly in the following language: {appLang.ToUpper()}.
                    2. Divide the syllabus exactly into {days} distinct days using the delimiter '|||DAY X|||'.
                    3. DO NOT write a massive wall of theory. Provide a structured list of main topics and sub-topics for each day.
                    4. Include an engaging introduction for each day.
                    5. FORMATTING: You MUST format important terms using **keyword** and key summary goals using __important goal__.

                    Base the outline strictly on these extracted documents:
                    {finalSourceText}";

            try
            {
                GeneratedPlan = await AiService.GetResponseAsync(fullPrompt);
                ProjectModel newProject = new ProjectModel
                {
                    ProjectName = subject,
                    RawPlan = GeneratedPlan,
                    Days = days,
                    Hours = hours,
                    UserNotes = notes,
                    CreatedAt = DateTime.Now
                };
                ProjectManager.SaveProject(newProject);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI Generation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                LoadingOverlay.Visibility = Visibility.Collapsed;
                MainContent.Opacity = 1.0;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}