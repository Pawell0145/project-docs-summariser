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

    public partial class StudyWindow : Window
    {
        private Dictionary<string, string> dayContents = new Dictionary<string, string>();

        public StudyWindow()
        {
            InitializeComponent();
        }

        public void LoadPlan(string rawPlan)
        {
            dayContents.Clear();
            DaysSidebarList.Items.Clear();

            int dayIndex = 0;
            string[] parts = rawPlan.Split(new string[] { "|||DAY " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                int markerIndex = part.IndexOf("|||");
                if (markerIndex != -1)
                {
                    string dayNumber = part.Substring(0, markerIndex).Trim();
                    string content = part.Substring(markerIndex + 3).Trim();

                    string dayTitle = $"Day #{dayNumber}";
                    dayContents[dayTitle] = content;

                    ListBoxItem item = new ListBoxItem();
                    item.Padding = new Thickness(15, 10, 15, 10);
                    item.Tag = dayTitle;

                    if (dayIndex == 0)
                    {
                        item.Content = $"☑️ {dayTitle}";
                        item.IsEnabled = true;
                    }
                    else
                    {
                        item.Content = $"❌ {dayTitle}";
                        item.IsEnabled = false;
                        item.Foreground = Brushes.Gray;
                    }

                    DaysSidebarList.Items.Add(item);
                    dayIndex++;
                }
            }

            ListBoxItem quizItem = new ListBoxItem();
            quizItem.Content = "Quiz";
            quizItem.Padding = new Thickness(15, 10, 15, 10);
            quizItem.Foreground = Brushes.DeepSkyBlue;
            quizItem.FontWeight = FontWeights.Bold;
            DaysSidebarList.Items.Add(quizItem);

            if (DaysSidebarList.Items.Count > 0)
            {
                DaysSidebarList.SelectedIndex = 0;
            }
        }

        private void DaysSidebarList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DaysSidebarList.SelectedItem is ListBoxItem selectedItem)
            {
                string selectedDay = selectedItem.Tag.ToString();

                if (selectedDay == "Quiz")
                {
                    CurrentDayTitle.Text = "QUIZ TIME";
                    StudyContentText.Text = "Dynamic quizzes will be generated here to test your weak spots!";
                }
                else if (dayContents.ContainsKey(selectedDay))
                {
                    CurrentDayTitle.Text = selectedDay.ToUpper();
                    StudyContentText.Text = dayContents[selectedDay];
                }
            }
        }

        private async void AskAiButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatInputTextBox.Text))
            {
                return;
            }

            if (DaysSidebarList.SelectedItem is ListBoxItem selectedItem)
            {
                string exactKey = selectedItem.Tag.ToString();

                if (dayContents.ContainsKey(exactKey))
                {
                    string text = dayContents[exactKey];
                    string question = ChatInputTextBox.Text;

                    AskAiButton.IsEnabled = false;
                    AskAiButton.Content = "Thinking...";

                    try
                    {
                        string prompt = $"You are an AI tutor. " +
                            $"Answer the student's question based strictly on this text:" +
                            $"\n\n{text}\n\nStudent's question: {question}";

                        string aiResponse = await AiService.GetResponseAsync(prompt);

                        StudyContentText.Text += $"\n\n---\nYour Question: {question}\n\nAI: {aiResponse}";

                        ChatInputTextBox.Clear();

                        if (StudyContentText.Parent is ScrollViewer scroll)
                        {
                            scroll.ScrollToEnd();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No connection with Ai: {ex.Message}");
                    }
                    finally
                    {
                        AskAiButton.IsEnabled = true;
                        AskAiButton.Content = "Ask AI";
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private void TakeQuizButton_Click(object sender, RoutedEventArgs e)
        {
            if (DaysSidebarList.SelectedItem is ListBoxItem selectedItem)
            {
                string exactKey = selectedItem.Tag.ToString();

                if (dayContents.ContainsKey(exactKey))
                {
                    string textToTest = dayContents[exactKey];
                    Hide();

                    QuizWindow quizWindow = new QuizWindow(textToTest);
                    quizWindow.ShowDialog();
                    Show();

                    if (quizWindow.PassedQuiz)
                    {
                        UnlockNextDay();
                    }
                }
            }
        }

        private void UnlockNextDay()
        {
            int currentIndex = DaysSidebarList.SelectedIndex;

            if (currentIndex >= 0)
            {
                ListBoxItem currentItem = (ListBoxItem)DaysSidebarList.Items[currentIndex];
                string currentTitle = currentItem.Tag?.ToString() ?? "";
                if (currentTitle != "Quiz")
                {
                    currentItem.Content = $"✅ {currentTitle}";
                }
            }

            if (currentIndex + 1 < DaysSidebarList.Items.Count)
            {
                ListBoxItem nextItem = (ListBoxItem)DaysSidebarList.Items[currentIndex + 1];
                string nextTitle = nextItem.Tag?.ToString() ?? "";

                if (nextTitle == "Quiz")
                {
                    nextItem.IsEnabled = true;
                }
                else
                {
                    nextItem.Content = $"🔓 {nextTitle}";
                    nextItem.IsEnabled = true;
                    nextItem.Foreground = Brushes.White;
                }
            }
        }
    }
}

