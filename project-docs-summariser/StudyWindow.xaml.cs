using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;

namespace project_docs_summariser
{
    public partial class StudyWindow : Window
    {
        private Dictionary<string, string> dayContents = new Dictionary<string, string>();
        private Dictionary<string, string> chatHistories = new Dictionary<string, string>();
        private bool isSidebarExpanded = true;
        private int totalDays;
        private int hoursPerDay;
        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        public StudyWindow()
        {
            InitializeComponent();
        }

        public void LoadPlan(string rawPlan, int days, int hours)
        {
            totalDays = days;
            hoursPerDay = hours;
            dayContents.Clear();
            chatHistories.Clear();
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

            if (dayContents.Count == 0)
            {
                var fallbackParts = Regex.Split(rawPlan, @"(?i)\bDAY\s+(\d+)[:\-]?");
                for (int i = 1; i < fallbackParts.Length; i += 2)
                {
                    string dayNum = fallbackParts[i].Trim();
                    string content = (i + 1 < fallbackParts.Length) ? fallbackParts[i + 1].Trim() : "";
                    if (content.StartsWith(":") || content.StartsWith("-"))
                    {
                        content = content.Substring(1).Trim();
                    }

                    string dayTitle = $"Day #{dayNum}";
                    dayContents[dayTitle] = content;

                    ListBoxItem item = new ListBoxItem();
                    item.Padding = new Thickness(15, 10, 15, 10);
                    item.Tag = dayTitle;

                    if (dayContents.Count == 1)
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
                }
            }

            if (dayContents.Count == 0)
            {
                string dayTitle = "Day #1";
                dayContents[dayTitle] = rawPlan.Trim();

                ListBoxItem item = new ListBoxItem();
                item.Padding = new Thickness(15, 10, 15, 10);
                item.Tag = dayTitle;
                item.Content = $"☑️ {dayTitle}";
                item.IsEnabled = true;
                DaysSidebarList.Items.Add(item);
            }

            ListBoxItem quizItem = new ListBoxItem();
            quizItem.Content = "🔒 Summary";
            quizItem.Foreground = Brushes.Gray;
            quizItem.IsEnabled = false;
            quizItem.Tag = "Summary";
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

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new System.Threading.CancellationTokenSource();

                if (selectedDay == "Summary")
                {
                    CurrentDayTitle.Text = "FINAL EXAM";
                    if (dayContents.ContainsKey("Summary"))
                    {
                        QuizButton.Content = "TRY AGAIN";
                        _ = AnimateAiResponseAsync(dayContents["Summary"], true, _cancellationTokenSource.Token);
                    }
                    else
                    {
                        QuizButton.Content = "START EXAM";
                        StudyContentText.Text = "You have unlocked the Final Exam! Click 'START EXAM' above to generate your assessment.";
                    }
                }
                else if (dayContents.ContainsKey(selectedDay))
                {
                    CurrentDayTitle.Text = selectedDay.ToUpper();
                    QuizButton.Content = "DAILY QUIZ";
                    string contentToLoad = dayContents[selectedDay];
                    if (chatHistories.ContainsKey(selectedDay) && !string.IsNullOrWhiteSpace(chatHistories[selectedDay]))
                    {
                        contentToLoad += "\n\n--- HISTORIA ROZMOWY ---\n\n" + chatHistories[selectedDay];
                    }

                    _ = AnimateAiResponseAsync(contentToLoad, true, _cancellationTokenSource.Token);
                }
            }
        }

        private async void AskAiButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatInputTextBox.Text)) return;

            if (DaysSidebarList.SelectedItem is ListBoxItem selectedItem)
            {
                string exactKey = selectedItem.Tag?.ToString() ?? "";

                if (dayContents.ContainsKey(exactKey))
                {
                    string text = dayContents[exactKey];
                    string question = ChatInputTextBox.Text;

                    AskAiButton.IsEnabled = false;
                    AskAiButton.Content = "Thinking...";
                    var separatorBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#444444");
                    var studentBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#B0B0B0");   

                    StudyContentText.Inlines.Add(new Run("\n\n--------------------------------------------------\n") { Foreground = separatorBrush });

                    StudyContentText.Inlines.Add(new Run($"Student: {question}\n") { FontWeight = FontWeights.Bold, Foreground = studentBrush });
                    if (StudyContentText.Parent is ScrollViewer sc) sc.ScrollToEnd();

                    ChatInputTextBox.Clear();

                    if (!chatHistories.ContainsKey(exactKey))
                    {
                        chatHistories[exactKey] = "";
                    }
                    string history = chatHistories[exactKey];

                    try
                    {
                        string prompt = $@"You are an interactive, engaging AI professor.
                            TODAY'S MATERIAL SYLLABUS: {text}
                            CONVERSATION HISTORY: {history}
                            STUDENT SAYS: '{question}'

                            CRITICAL INSTRUCTIONS:
                            1. DO NOT output a massive wall of text. It's boring. 
                            2. Teach ONE or TWO core concepts at a time. Explain them deeply and engagingly, using examples.
                            3. ALWAYS end your response by giving the student a choice. Ask them: 'Which of these topics would you like to explore next?' and provide 2-3 bulleted options based on the syllabus.
                            4. MANDATORY FORMATTING: You MUST strictly format crucial keywords using **keyword** and the most important sentences using __important sentence__. Do not forget the underscores!
                            5. Do not repeat topics the student has already learned or confirmed.";

                        string aiResponse = await AiService.GetResponseAsync(prompt);
                        chatHistories[exactKey] += $"Student: {question}\nAI: {aiResponse}\n\n";

                        _cancellationTokenSource?.Cancel();
                        _cancellationTokenSource = new System.Threading.CancellationTokenSource();
                        await AnimateAiResponseAsync(aiResponse, false, _cancellationTokenSource.Token);
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
            }
        }

        private async void TakeQuizButton_Click(object sender, RoutedEventArgs e)
        {
            if (DaysSidebarList.SelectedItem is ListBoxItem selectedItem)
            {
                string exactKey = selectedItem.Tag.ToString();

                if (exactKey == "Summary")
                {
                    await GenerateAndStartFinalExam();
                }
                else if (dayContents.ContainsKey(exactKey))
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
                UnlockNextDay();
            }
        }

        private async Task GenerateAndStartFinalExam()
        {
            QuizButton.IsEnabled = false;
            QuizButton.Content = "GENERATING...";
            DaysSidebarList.IsEnabled = false;
            StudyContentText.Text = "AI is analyzing your entire material and preparing the final academic assessment. Please wait...";

            try
            {
                int taskCount = Math.Min(15, Math.Max(5, (totalDays * hoursPerDay)));

                string allDaysText = string.Join("\n\n", dayContents.Values);
                string prompt = $@"You are an elite University Examiner. 
                            CONTEXT: This is a final exam for a course that lasted {totalDays} days, {hoursPerDay} hours per day.

                            STUDY MATERIAL:
                            {allDaysText}

                            INSTRUCTIONS:
                            1. Generate EXACTLY {taskCount} tasks.
                            2. Analyze the material and pick ONLY 2-3 most relevant TaskTypes from the list: [MultipleChoice, ShortAnswer, Essay, Calculation, CaseStudy, FillInTheBlanks, CodeSnippet].
                               - CRITICAL: If the subject is Humanistic, DO NOT use Calculation or CodeSnippet. 
                               - If the subject is Technical, prioritize Calculation/Code/ShortAnswer.
                            3. You MUST return ONLY a valid JSON object. Do not include any extraneous text.

                            JSON SCHEMA EXAMPLES (Use exact TaskType values: MultipleChoice, ShortAnswer, Essay, Calculation, CaseStudy, FillInTheBlanks, CodeSnippet):

                            {{
                              ""DetectedSubject"": ""Computer Science - Algorithms"",
                              ""Tasks"": [
                                {{
                                  ""Type"": ""MultipleChoice"",
                                  ""Instruction"": ""Which sorting algorithm has O(n log n) worst-case time complexity?"",
                                  ""Options"": [""Merge Sort"", ""Quick Sort"", ""Bubble Sort""]
                                }},
                                {{
                                  ""Type"": ""ShortAnswer"",
                                  ""Instruction"": ""Define polymorphism in one sentence."",
                                  ""Options"": []
                                }}
                              ]
                            }}";

                string aiResponse = await AiService.GetResponseAsync(prompt);
                int startIndex = aiResponse.IndexOf('{');
                int endIndex = aiResponse.LastIndexOf('}');

                if (startIndex >= 0 && endIndex >= startIndex)
                {
                    aiResponse = aiResponse.Substring(startIndex, endIndex - startIndex + 1);
                }

                var examOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var exam = System.Text.Json.JsonSerializer.Deserialize<SummaryGeneration.SummaryExam>(aiResponse, examOptions);

                if (exam != null && exam.Tasks.Count > 0)
                {
                    this.Hide();

                    SummaryWindow summaryWin = new SummaryWindow(exam);
                    summaryWin.ShowDialog();

                    this.Show();
                    if (!string.IsNullOrEmpty(summaryWin.FinalGradingReport))
                    {
                        dayContents["Summary"] = summaryWin.FinalGradingReport;
                        QuizButton.Content = "TRY AGAIN";
                        _cancellationTokenSource?.Cancel();
                        _cancellationTokenSource = new System.Threading.CancellationTokenSource();
                        await AnimateAiResponseAsync(summaryWin.FinalGradingReport, true, _cancellationTokenSource.Token);
                    }
                    else
                    {
                        StudyContentText.Text = "Exam was cancelled. Click 'START EXAM' to try again.";
                        QuizButton.Content = "START EXAM";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating exam: {ex.Message}");
                StudyContentText.Text = "Failed to generate the exam. Try again later.";
                QuizButton.Content = "START EXAM";
            }
            finally
            {
                QuizButton.IsEnabled = true;
                DaysSidebarList.IsEnabled = true;
            }
        }

        private void UnlockNextDay()
        {
            int currentIndex = DaysSidebarList.SelectedIndex;

            if (currentIndex >= 0)
            {
                ListBoxItem currentItem = (ListBoxItem)DaysSidebarList.Items[currentIndex];
                string currentTitle = currentItem.Tag?.ToString() ?? "";
                if (currentTitle != "Summary")
                {
                    currentItem.Content = $"✅ {currentTitle}";
                }
            }

            if (currentIndex + 1 < DaysSidebarList.Items.Count)
            {
                ListBoxItem nextItem = (ListBoxItem)DaysSidebarList.Items[currentIndex + 1];
                string nextTitle = nextItem.Tag?.ToString() ?? "";

                if (nextTitle == "Summary")
                {
                    nextItem.Content = "🎓 Summary";
                    nextItem.Foreground = Brushes.DeepSkyBlue;
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

        private async Task AnimateAiResponseAsync(string response, bool isInitialLoad, System.Threading.CancellationToken token)
        {
            if (isInitialLoad)
            {
                StudyContentText.Inlines.Clear();
            }
            else
            {
                StudyContentText.Inlines.Add(new Run("\nAI: ") { FontWeight = FontWeights.Bold, Foreground = Brushes.DeepSkyBlue });
            }

            string[] parts = Regex.Split(response, @"(\*\*.*?\*\*|__.*?__)");

            var softOrange = (SolidColorBrush)new BrushConverter().ConvertFrom("#DCA550");
            var softCyan = (SolidColorBrush)new BrushConverter().ConvertFrom("#4EC9B0");
            bool disableAnimations = AreAnimationsDisabled;

            foreach (string part in parts)
            {
                if (token.IsCancellationRequested) break;
                if (string.IsNullOrEmpty(part)) continue;

                Run run = new Run();
                string textToPrint = "";

                if (part.StartsWith("**") && part.EndsWith("**"))
                {
                    textToPrint = part.Substring(2, part.Length - 4);
                    run.FontWeight = FontWeights.Bold;
                    run.Foreground = softOrange;
                }
                else if (part.StartsWith("__") && part.EndsWith("__"))
                {
                    textToPrint = part.Substring(2, part.Length - 4);
                    run.FontStyle = FontStyles.Italic;
                    run.Foreground = softCyan;
                }
                else
                {
                    textToPrint = part;
                    run.Foreground = Brushes.LightGray;
                }

                if (isInitialLoad || disableAnimations)
                {
                    run.Text = textToPrint;
                    StudyContentText.Inlines.Add(run);

                    if (!isInitialLoad && StudyContentText.Parent is ScrollViewer scroll)
                    {
                        scroll.ScrollToEnd();
                    }

                    continue;
                }

                StudyContentText.Inlines.Add(run);
                foreach (char c in textToPrint)
                {
                    if (token.IsCancellationRequested) break;

                    run.Text += c;
                    if (StudyContentText.Parent is ScrollViewer scroll) scroll.ScrollToEnd();
                    await Task.Delay(5);
                }
            }
        }

        private async void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            btn.IsEnabled = false;

            double targetWidth = isSidebarExpanded ? 55 : 160;
            double targetHeight = isSidebarExpanded ? 55 : MainContainerGrid.ActualHeight;

            if (double.IsNaN(SidebarBorder.Height))
            {
                SidebarBorder.Height = MainContainerGrid.ActualHeight;
            }

            TextBlock menuText = (TextBlock)btn.Template.FindName("MenuText", btn);

            if (AreAnimationsDisabled)
            {
                SidebarBorder.Width = targetWidth;
                SidebarBorder.Height = targetHeight;

                if (menuText != null)
                {
                    menuText.Opacity = isSidebarExpanded ? 0 : 1;
                }

                AnimateIconToX(btn, false);

                isSidebarExpanded = !isSidebarExpanded;
                btn.IsEnabled = true;
                return;
            }

            System.Windows.Media.Animation.DoubleAnimation widthAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromSeconds(0.35),
                EasingFunction = new System.Windows.Media.Animation.QuarticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };
            System.Windows.Media.Animation.DoubleAnimation heightAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = targetHeight,
                Duration = TimeSpan.FromSeconds(0.35),
                EasingFunction = new System.Windows.Media.Animation.QuarticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };
            System.Windows.Media.Animation.DoubleAnimation textFade = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = isSidebarExpanded ? 0 : 1,
                Duration = TimeSpan.FromSeconds(0.2)
            };

            SidebarBorder.BeginAnimation(WidthProperty, widthAnim);
            SidebarBorder.BeginAnimation(HeightProperty, heightAnim);

            if (menuText != null) menuText.BeginAnimation(UIElement.OpacityProperty, textFade);

            AnimateIconToX(btn, true);

            await Task.Delay(400);

            AnimateIconToX(btn, false);

            isSidebarExpanded = !isSidebarExpanded;
            btn.IsEnabled = true;
        }

        private void AnimateIconToX(Button btn, bool toX)
        {
            Rectangle topBar = (Rectangle)btn.Template.FindName("TopBar", btn);
            Rectangle midBar = (Rectangle)btn.Template.FindName("MidBar", btn);
            Rectangle bottomBar = (Rectangle)btn.Template.FindName("BottomBar", btn);

            if (topBar == null || midBar == null || bottomBar == null) return;

            if (topBar.RenderTransform.IsFrozen)
                topBar.RenderTransform = topBar.RenderTransform.Clone();

            if (bottomBar.RenderTransform.IsFrozen)
                bottomBar.RenderTransform = bottomBar.RenderTransform.Clone();

            double angle = toX ? 45 : 0;
            double yTranslate = toX ? 6 : 0;
            double opacity = toX ? 0 : 1;
            TimeSpan duration = TimeSpan.FromSeconds(0.15);

            TransformGroup topGroup = (TransformGroup)topBar.RenderTransform;
            RotateTransform topRot = (RotateTransform)topGroup.Children[0];
            TranslateTransform topTrans = (TranslateTransform)topGroup.Children[1];

            TransformGroup botGroup = (TransformGroup)bottomBar.RenderTransform;
            RotateTransform botRot = (RotateTransform)botGroup.Children[0];
            TranslateTransform botTrans = (TranslateTransform)botGroup.Children[1];

            if (AreAnimationsDisabled)
            {
                topRot.Angle = angle;
                topTrans.Y = yTranslate;
                midBar.Opacity = opacity;

                botRot.Angle = -angle;
                botTrans.Y = -yTranslate;
                return;
            }

            topRot.BeginAnimation(RotateTransform.AngleProperty, new System.Windows.Media.Animation.DoubleAnimation(angle, duration));
            topTrans.BeginAnimation(TranslateTransform.YProperty, new System.Windows.Media.Animation.DoubleAnimation(yTranslate, duration));

            midBar.BeginAnimation(UIElement.OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(opacity, duration));

            botRot.BeginAnimation(RotateTransform.AngleProperty, new System.Windows.Media.Animation.DoubleAnimation(-angle, duration));
            botTrans.BeginAnimation(TranslateTransform.YProperty, new System.Windows.Media.Animation.DoubleAnimation(-yTranslate, duration));
        }

        private void ChatInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    return;
                }
                else
                {
                    e.Handled = true;
                    if (AskAiButton.IsEnabled)
                    {
                        AskAiButton_Click(AskAiButton, new RoutedEventArgs());
                    }
                }
            }
        }

        private bool AreAnimationsDisabled
        {
            get { return Properties.Settings.Default.DisableAnimations; }
        }
    }
}