using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace project_docs_summariser
{

    public partial class QuizWindow : Window
    {
        private List<QuizQuestion> questions = new List<QuizQuestion>();
        private int currentQuestionIndex = 0;
        private int score = 0;
        public bool PassedQuiz { get; private set; } = false;
        public QuizWindow(string textToTest)
        {
            InitializeComponent();
            GenerateQuiz(textToTest);
        }

        private async void GenerateQuiz(string text)
        {
            NextQuestionBtn.IsEnabled = false;
            QuestionTextBlock.Text = "AI is generating your quiz. Please wait...";

            try
            {
                string prompt = $"You are an AI tutor. Generate one multiple-choice questions based strictly on the following text:\n\n{text}\n\n" +
                @"You MUST return ONLY a valid JSON array of objects. Do not include any extraneous text (e.g., 'Here is your JSON') and do NOT wrap it in markdown code blocks like ```json. " +
                @"Follow this exact JSON format:
                    [
                      {
                        ""QuestionText"": ""Content of the question?"",
                        ""Options"": [""Answer A"", ""Answer B"", ""Answer C"", ""Answer D""],
                        ""CorrectOptionIndex"": 1
                      }
                    ]";

                string aiResponse = await AiService.GetResponseAsync(prompt);

                questions = System.Text.Json.JsonSerializer.Deserialize<List<QuizQuestion>>(aiResponse);

                if (questions != null && questions.Count > 0)
                {
                    LoadCurrentQues();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating quiz: {ex.Message}");
                this.Close();
            }
        }

        private void LoadCurrentQues()
        {
            NextQuestionBtn.IsEnabled = true;

            QuizQuestion currentQues = questions[currentQuestionIndex];
            CurrentQuesTitle.Text = "QUESTION #" + (currentQuestionIndex + 1);

            QuestionTextBlock.Text = currentQues.QuestionText;

            OptionA.Content = currentQues.Options[0];
            OptionB.Content = currentQues.Options[1];
            OptionC.Content = currentQues.Options[2];
            OptionD.Content = currentQues.Options[3];

            OptionA.IsChecked = false;
            OptionB.IsChecked = false;
            OptionC.IsChecked = false;
            OptionD.IsChecked = false;

            QuizProgressBar.Maximum = questions.Count;
            QuizProgressBar.Value = currentQuestionIndex;
        }

        private void NextQuestionBtn_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = -1;

            if (OptionA.IsChecked == true)
            {
                selectedIndex = 0;
            }
            else if (OptionB.IsChecked == true)
            {
                selectedIndex = 1;
            }
            else if (OptionC.IsChecked == true)
            {
                selectedIndex = 2;
            }
            else if (OptionD.IsChecked == true) 
            {
                selectedIndex = 3;
            }

            if (selectedIndex == -1)
            {
                MessageBox.Show("Please select an answer!", "No Answer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedIndex == questions[currentQuestionIndex].CorrectOptionIndex)
            {
                score++;
            }

            currentQuestionIndex++;
            if (currentQuestionIndex < questions.Count)
            {
                LoadCurrentQues();
            }
            else
            {
                ShowResults();
            }
        }

        private void ShowResults()
        {
            QuizStartGrid.Visibility = Visibility.Collapsed;
            QuizResultGrid.Visibility = Visibility.Visible;

            ResultScore.Text = $"{score} / {questions.Count}";
            if (score == questions.Count)
            {
                ResultTitle.Text = "CONGRATULATIONS!";
                ResultTitle.Foreground = Brushes.LimeGreen;
                PassedQuiz = true;
            }
            else
            {
                ResultTitle.Text = "TRY AGAIN!";
                ResultTitle.Foreground = Brushes.OrangeRed;
            }
        }

        private void FinishQuiz(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void YourAnswers(object sender, RoutedEventArgs e)
        {
            string summary = "Correct answers:\n\n";
            for (int i = 0; i < questions.Count; i++)
            {
                int correctIndex = questions[i].CorrectOptionIndex;
                string correctText = questions[i].Options[correctIndex];
                summary += $"Q{i + 1}: {correctText}\n";
            }
            MessageBox.Show(summary, "Review", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TryAgain(object sender, RoutedEventArgs e)
        {
            score = 0;
            currentQuestionIndex = 0;

            QuizResultGrid.Visibility = Visibility.Collapsed;
            QuizStartGrid.Visibility = Visibility.Visible;

            LoadCurrentQues();
        }
    }
    
}
