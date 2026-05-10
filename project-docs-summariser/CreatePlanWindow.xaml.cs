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
            string fullPrompt = $@"You are an elite academic planner.
                    Create a highly structured SYLLABUS/OUTLINE for the subject: {subject}.
                    Total Timeframe: {days} days, studying {hours} hours per day.
                    Additional Preferences/Notes: {notes}

                    CRITICAL INSTRUCTIONS:
                    1. Divide the syllabus exactly into {days} distinct days using the delimiter '|||DAY X|||'.
                    2. DO NOT write a massive wall of theory. Instead, provide a structured list of main topics and sub-topics for each day.
                    3. Include an engaging introduction for each day. Example: 'Today we have {hours} hours scheduled. We will cover X, Y, and Z.'
                    4. Provide a rich, detailed outline of the concepts to be studied, so the Interactive Tutor AI can use this outline to teach the student step-by-step later.
                    5. FORMATTING: You MUST format important terms using **keyword** and key summary goals using __important goal__.

                    Base the outline strictly on these extracted documents:
                    {accumulatedSourceText}";

            try
            {
                GeneratedPlan = await AiService.GetResponseAsync(fullPrompt);
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