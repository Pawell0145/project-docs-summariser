using project_docs_summariser;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace WpfAiIntegration
{
    public partial class CreatePlanWindow : Window
    {
        private string accumulatedSourceText = "";

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
                        string extractedText = DocumentExtractor.ExtractText(filePath);

                        if (!string.IsNullOrWhiteSpace(extractedText))
                        {
                            string fileName = Path.GetFileName(filePath);
                            accumulatedSourceText += $"\n\n--- SOURCE DOCUMENT: {fileName} ---\n{extractedText}";
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

        private async void CreatePlan_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TopicTextBox.Text))
            {
                MessageBox.Show("Please enter a subject before creating a plan.", "Missing Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsEnabled = false;

            string subject = TopicTextBox.Text;
            string daysText = DaysTextBox.Text;
            string hoursText = HoursTextBox.Text;
            string notes = NotesTextBox.Text;

            int days = 0;
            int hours = 0;
            int.TryParse(daysText, out days);
            int.TryParse(hoursText, out hours);

            string fullPrompt = $@"Create a detailed study plan for the subject: {subject}.
Total Timeframe: {days} days, studying {hours} hours per day.
Additional Preferences/Notes: {notes}

CRITICAL INSTRUCTION FOR PARSING: You MUST divide the study plan explicitly into exactly {days} distinct days. 
You MUST start each day's section exactly with the special delimiter '|||DAY X|||' (for example: |||DAY 1|||, |||DAY 2|||, |||DAY 3|||). 
Do NOT use standard headers like 'Day 1:'. Our automated parser strictly requires the '|||DAY X|||' marker at the beginning of each day's content.

Base the study materials, topics, and summaries strictly on the following extracted source documents:
{accumulatedSourceText}";

            try
            {
                GeneratedPlan = await AiService.GetResponseAsync(fullPrompt);

                // Permanently save the newly generated plan locally to history
                ProjectModel newProject = new ProjectModel
                {
                    ProjectName = subject,
                    RawPlan = GeneratedPlan,
                    Days = days,
                    Hours = hours,
                    CreatedAt = DateTime.Now
                };
                ProjectManager.SaveProject(newProject);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI Generation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IsEnabled = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}