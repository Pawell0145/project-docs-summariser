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
        private string sourceText = "";
        private List<int> userSelectedIndices = new List<int>();
        private bool isReviewMode = false;

        public QuizWindow(string textToTest)
        {
            InitializeComponent();
            sourceText = textToTest;
            GenerateQuiz(textToTest);
        }

        private async void GenerateQuiz(string text)
        {
            NextQuestionBtn.IsEnabled = false;
            QuestionTextBlock.Text = "AI is generating your quiz. Please wait...";

            try
            {
                string prompt = $"You are an AI tutor. Generate three UNIQUE, RANDOM, and NEW multiple-choice questions based strictly on the following text:\n\n{text}\n\n" +
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

            RadioButton[] options = { OptionA, OptionB, OptionC, OptionD };
            var defaultBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#2D2D30");
            var correctBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#1A5D1A"); 
            var wrongBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#8B0000"); 

            for (int i = 0; i < options.Length; i++)
            {
                options[i].Content = currentQues.Options[i];
                options[i].IsChecked = false;
                options[i].IsEnabled = !isReviewMode;
                options[i].Background = defaultBrush;
            }

            if (isReviewMode)
            {
                int correctIdx = currentQues.CorrectOptionIndex;
                int userIdx = userSelectedIndices[currentQuestionIndex];

                options[correctIdx].Background = correctBrush;

                if (userIdx != -1 && userIdx != correctIdx)
                {
                    options[userIdx].Background = wrongBrush;
                    options[userIdx].IsChecked = true;
                }
                else if (userIdx == correctIdx)
                {
                    options[userIdx].IsChecked = true;
                }

                PreviousBtn.Visibility = currentQuestionIndex == 0 ? Visibility.Hidden : Visibility.Visible;
                NextQuestionBtn.Content = (currentQuestionIndex == questions.Count - 1) ? "CLOSE" : "NEXT";
            }
            else
            {
                PreviousBtn.Visibility = Visibility.Hidden;
                NextQuestionBtn.Content = "NEXT";
            }

            QuizProgressBar.Maximum = questions.Count;
            System.Windows.Media.Animation.DoubleAnimation anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = currentQuestionIndex,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            QuizProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, anim);
        }

        private async void NextQuestionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isReviewMode && NextQuestionBtn.Content.ToString() == "CLOSE")
            {
                System.Windows.Media.Animation.DoubleAnimation reviewAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = questions.Count,
                    Duration = TimeSpan.FromSeconds(0.4),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                QuizProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, reviewAnim);

                NextQuestionBtn.IsEnabled = false;
                await Task.Delay(500);

                isReviewMode = false;
                QuizStartGrid.Visibility = Visibility.Collapsed;
                QuizResultGrid.Visibility = Visibility.Visible;
                return;
            }
            int selectedIndex = -1;

            if (OptionA.IsChecked == true) selectedIndex = 0;
            else if (OptionB.IsChecked == true) selectedIndex = 1;
            else if (OptionC.IsChecked == true) selectedIndex = 2;
            else if (OptionD.IsChecked == true) selectedIndex = 3;

            if (!isReviewMode)
            {
                if (selectedIndex == -1)
                {
                    MessageBox.Show("Please select an answer!", "No Answer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                userSelectedIndices.Add(selectedIndex);

                if (selectedIndex == questions[currentQuestionIndex].CorrectOptionIndex)
                {
                    score++;
                }
            }
            currentQuestionIndex++;

            if (currentQuestionIndex < questions.Count)
            {
                LoadCurrentQues();
            }
            else if (!isReviewMode)
            {
                System.Windows.Media.Animation.DoubleAnimation finalAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = questions.Count,
                    Duration = TimeSpan.FromSeconds(0.4),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                QuizProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, finalAnim);
                NextQuestionBtn.IsEnabled = false;
                await Task.Delay(500);
                ShowResults();
            }
        }
        private void ShowResults()
        {
            QuizStartGrid.Visibility = Visibility.Collapsed;
            QuizResultGrid.Visibility = Visibility.Visible;

            double percentage = ((double)score / questions.Count) * 100;
            ResultScore.Text = $"{score} / {questions.Count}";

            if (percentage >= 60)
            {
                ResultTitle.Text = "PASSED!";
                ResultTitle.Foreground = Brushes.LimeGreen;
                PassedQuiz = true;
                TryAgainBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                ResultTitle.Text = "TRY AGAIN!";
                ResultTitle.Foreground = Brushes.OrangeRed;
                PassedQuiz = false;
                TryAgainBtn.Visibility = Visibility.Visible;
            }
        }
        private void FinishQuiz(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void YourAnswers(object sender, RoutedEventArgs e)
        {
            isReviewMode = true;
            currentQuestionIndex = 0;

            QuizResultGrid.Visibility = Visibility.Collapsed;
            QuizStartGrid.Visibility = Visibility.Visible;

            LoadCurrentQues();
        }

        private void TryAgain(object sender, RoutedEventArgs e)
        {
            score = 0;
            currentQuestionIndex = 0;
            userSelectedIndices.Clear();

            QuizResultGrid.Visibility = Visibility.Collapsed;
            QuizStartGrid.Visibility = Visibility.Visible;

            OptionA.Content = ""; OptionB.Content = ""; OptionC.Content = ""; OptionD.Content = "";
            OptionA.IsChecked = false; OptionB.IsChecked = false; OptionC.IsChecked = false; OptionD.IsChecked = false;
            QuizProgressBar.Value = 0;
            CurrentQuesTitle.Text = "GENERATING NEW QUIZ...";

            GenerateQuiz(sourceText);
        }

        private void PreviousBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentQuestionIndex > 0)
            {
                currentQuestionIndex--;
                LoadCurrentQues();
            }
        }

        private void Option_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isReviewMode) return;

            RadioButton rb = sender as RadioButton;
            if (rb.IsChecked == true)
            {
                rb.IsChecked = false;
                e.Handled = true;
            }
        }

        private void Option_StateChanged(object sender, RoutedEventArgs e)
        {
            if (isReviewMode) return;

            RadioButton[] options = { OptionA, OptionB, OptionC, OptionD };
            var defaultBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#2D2D30");
            var selectedBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#007ACC");

            foreach (var opt in options)
            {
                opt.Background = (opt.IsChecked == true) ? selectedBrush : defaultBrush;
            }
        }
    }
    
}
