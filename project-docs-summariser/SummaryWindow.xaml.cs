using project_docs_summariser;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace project_docs_summariser
{
    public partial class SummaryWindow : Window
    {
        private SummaryGeneration.SummaryExam currentExam;
        public string FinalGradingReport { get; private set; }

        public SummaryWindow(SummaryGeneration.SummaryExam exam)
        {
            InitializeComponent();
            currentExam = exam;
            SubjectTitle.Text = $"FINAL EXAM: {exam.DetectedSubject.ToUpper()}";
            BuildExamUI();
        }

        private void BuildExamUI()
        {
            TasksContainer.Children.Clear();

            for (int i = 0; i < currentExam.Tasks.Count; i++)
            {
                var task = currentExam.Tasks[i];
                Border taskCard = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFrom("#252526"),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20),
                    Margin = new Thickness(0, 0, 0, 20),
                    BorderBrush = (Brush)new BrushConverter().ConvertFrom("#333337"),
                    BorderThickness = new Thickness(1)
                };
                StackPanel taskPanel = new StackPanel();

                TextBlock instructionText = new TextBlock
                {
                    Text = $"Task {i + 1} ({task.Type}):\n{task.Instruction}",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                taskPanel.Children.Add(instructionText);

                if (task.Type == SummaryGeneration.TaskType.MultipleChoice)
                {
                    StackPanel optionsPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
                    foreach (var option in task.Options)
                    {
                        RadioButton rb = new RadioButton
                        {
                            Content = option,
                            Foreground = Brushes.LightGray,
                            FontSize = 15,
                            Margin = new Thickness(0, 5, 0, 5),
                            GroupName = $"Question_{i}"
                        };
                        optionsPanel.Children.Add(rb);
                    }
                    taskPanel.Children.Add(optionsPanel);
                }
                else if (task.Type == SummaryGeneration.TaskType.Essay ||
                         task.Type == SummaryGeneration.TaskType.CaseStudy ||
                         task.Type == SummaryGeneration.TaskType.CodeSnippet)
                {
                    TextBox tb = new TextBox
                    {
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        Height = 150,
                        FontSize = 15,
                        Background = (Brush)new BrushConverter().ConvertFrom("#1E1E1E"),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(1),
                        BorderBrush = (Brush)new BrushConverter().ConvertFrom("#3F3F46"),
                        Padding = new Thickness(10),
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                    taskPanel.Children.Add(tb);
                }
                else
                {
                    TextBox tb = new TextBox
                    {
                        Height = 40,
                        FontSize = 15,
                        Background = (Brush)new BrushConverter().ConvertFrom("#1E1E1E"),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(1),
                        BorderBrush = (Brush)new BrushConverter().ConvertFrom("#3F3F46"),
                        Padding = new Thickness(10, 8, 10, 8)
                    };
                    taskPanel.Children.Add(tb);
                }
                taskCard.Child = taskPanel;
                TasksContainer.Children.Add(taskCard);
            }
        }

        private async void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            SubmitBtn.IsEnabled = false;
            SubmitBtn.Content = "GRADING IN PROGRESS. PLEASE WAIT...";

            string userAnswersText = "USER EXAM SUBMISSION:\n\n";
            int maxTotalScore = 0;

            for (int i = 0; i < currentExam.Tasks.Count; i++)
            {
                var task = currentExam.Tasks[i];
                int taskPoints = 1;
                if (task.Type == SummaryGeneration.TaskType.Essay || task.Type == SummaryGeneration.TaskType.CaseStudy)
                    taskPoints = 5;
                else if (task.Type == SummaryGeneration.TaskType.Calculation || task.Type == SummaryGeneration.TaskType.CodeSnippet)
                    taskPoints = 3;
                else if (task.Type == SummaryGeneration.TaskType.ShortAnswer || task.Type == SummaryGeneration.TaskType.FillInTheBlanks)
                    taskPoints = 2;

                maxTotalScore += taskPoints;
                userAnswersText += $"Task {i + 1} ({task.Type}) - MAX POINTS FOR THIS TASK: {taskPoints}\n";
                userAnswersText += $"Instruction: {task.Instruction}\n";

                var card = (Border)TasksContainer.Children[i];
                var taskPanel = (StackPanel)card.Child;
                string userAnswer = "No answer provided.";

                if (task.Type == SummaryGeneration.TaskType.MultipleChoice)
                {
                    var optionsPanel = (StackPanel)taskPanel.Children[1];
                    foreach (RadioButton rb in optionsPanel.Children)
                    {
                        if (rb.IsChecked == true)
                        {
                            userAnswer = rb.Content.ToString();
                            break;
                        }
                    }
                }
                else
                {
                    var tb = (TextBox)taskPanel.Children[1];
                    if (!string.IsNullOrWhiteSpace(tb.Text))
                    {
                        userAnswer = tb.Text;
                    }
                }
                userAnswersText += $"User Answer: {userAnswer}\n\n";
            }

            string gradingPrompt = $@"You are a strict but fair University Professor. Grade the following exam submission.
                        Subject: {currentExam.DetectedSubject}.
                        TOTAL EXAM MAX SCORE: {maxTotalScore} points.

                        {userAnswersText}

                        INSTRUCTIONS FOR FORMATTING AND GRADING:
                        1. Start with a massive header EXACTLY like this: **FINAL SCORE: [Points Earned]/{maxTotalScore}** and a short summary of the student's performance.
                        2. Grade each task based strictly on its assigned MAX POINTS. 
                           - If perfect, award full points.
                           - If partially correct, award partial points.
                           - If 'No answer provided.', award 0 points.
                        3. Break down the feedback task by task. Use double line breaks between tasks!
                        4. Format each task exactly like this:
                           **Task 1 ([Points Earned]/[Max Points for task] pts):** [Short instruction summary]
                           __Your Answer:__ [user's answer]
                           **Feedback:** [Your detailed feedback, corrections, and theory]
                        5. Use **bolding** for all key terms and __italics__ for emphasis. Make it highly readable.";

            try
            {
                string aiFeedback = await AiService.GetResponseAsync(gradingPrompt);
                FinalGradingReport = aiFeedback;

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during grading: {ex.Message}");
                SubmitBtn.IsEnabled = true;
                SubmitBtn.Content = "SUBMIT EXAM FOR GRADING";
            }
        }
    }
}