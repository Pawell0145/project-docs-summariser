using project_docs_summariser;
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
using System.Text.Json;

namespace project_docs_summariser
{
    public partial class StudyWindow : Window
    {
        private Dictionary<string, string> dayContents = new Dictionary<string, string>();
        private bool isSidebarExpanded = true;
        private int totalDays;
        private int hoursPerDay;
        private string currentProjectPath;
        private List<int> finishedDayIndices = new List<int>();
        private System.Threading.CancellationTokenSource _cancellationTokenSource;
        private string studentPreferences = "";

        public StudyWindow()
        {
            InitializeComponent();
        }

        public void LoadPlan(ProjectModel project)
        {
            totalDays = project.Days;
            hoursPerDay = project.Hours;
            currentProjectPath = project.FilePath;
            finishedDayIndices = project.CompletedDays ?? new List<int>();
            studentPreferences = project.UserNotes ?? "";

            dayContents.Clear();
            DaysSidebarList.Items.Clear();

            int dayIndex = 0;
            string[] parts = project.RawPlan.Split(new string[] { "|||DAY " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                int markerIndex = part.IndexOf("|||");
                if (markerIndex != -1)
                {
                    string dayNumber = part.Substring(0, markerIndex).Trim();
                    string content = part.Substring(markerIndex + 3).Trim();

                    if (dayNumber == "Summary")
                    {
                        dayContents["Summary"] = content;
                        continue;
                    }

                    string dayTitle = $"Day #{dayNumber}";
                    dayContents[dayTitle] = content;

                    ListBoxItem item = new ListBoxItem();
                    item.Padding = new Thickness(15, 10, 15, 10);
                    item.Tag = dayTitle;

                    if (finishedDayIndices.Contains(dayIndex))
                    {
                        item.Content = $"✅ {dayTitle}";
                        item.IsEnabled = true;
                    }
                    else if (dayIndex == 0 || (finishedDayIndices.Count > 0 && dayIndex == finishedDayIndices[finishedDayIndices.Count - 1] + 1))
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
            quizItem.Tag = "Summary";

            int actualStudyDaysCount = 0;
            foreach (var key in dayContents.Keys)
            {
                if (key.StartsWith("Day #")) actualStudyDaysCount++;
            }

            if (finishedDayIndices.Count >= actualStudyDaysCount && actualStudyDaysCount > 0)
            {
                quizItem.Content = "🎓 Summary";
                quizItem.Foreground = Brushes.DeepSkyBlue;
                quizItem.IsEnabled = true;
            }
            else
            {
                quizItem.Content = "🔒 Summary";
                quizItem.Foreground = Brushes.Gray;
                quizItem.IsEnabled = false;
            }
            DaysSidebarList.Items.Add(quizItem);

            if (DaysSidebarList.Items.Count > 0)
            {
                DaysSidebarList.SelectedIndex = 0;
            }
        }

        private void SaveCurrentPlanToDisk()
        {
            if (string.IsNullOrEmpty(currentProjectPath)) return;

            string updatedRawPlan = "";
            foreach (var kvp in dayContents)
            {
                string key = kvp.Key;
                string content = kvp.Value;

                if (key.StartsWith("Day #"))
                {
                    string dayNum = key.Substring(5).Trim();
                    updatedRawPlan += $"|||DAY {dayNum}|||\n{content}\n\n";
                }
                else if (key == "Summary")
                {
                    updatedRawPlan += $"|||DAY Summary|||\n{content}\n\n";
                }
            }

            ProjectManager.UpdateProjectPlan(currentProjectPath, updatedRawPlan);
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
                    _ = AnimateAiResponseAsync(dayContents[selectedDay], true, _cancellationTokenSource.Token);
                }
            }
        }

        private async void AskAiButton_Click(object sender, RoutedEventArgs e)
        {
            if (AskAiButton.Content.ToString() == "Stop")
            {
                _cancellationTokenSource?.Cancel();
                AskAiButton.Content = "Ask AI";
                StudyContentText.Inlines.Add(new Run("\n[AI stopped by user]") { Foreground = Brushes.Gray, FontStyle = FontStyles.Italic });
                return;
            }

            if (string.IsNullOrWhiteSpace(ChatInputTextBox.Text)) return;

            if (DaysSidebarList.SelectedItem is ListBoxItem selectedItem)
            {
                string exactKey = selectedItem.Tag.ToString();

                if (dayContents.ContainsKey(exactKey))
                {
                    string text = dayContents[exactKey];
                    string question = ChatInputTextBox.Text;

                    AskAiButton.Content = "Stop";

                    var separatorBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#444444");
                    var studentBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#B0B0B0");

                    StudyContentText.Inlines.Add(new Run("\n\n--------------------------------------------------\n") { Foreground = separatorBrush });
                    StudyContentText.Inlines.Add(new Run($"Student: {question}\n") { FontWeight = FontWeights.Bold, Foreground = studentBrush });
                    if (StudyContentText.Parent is ScrollViewer sc) sc.ScrollToEnd();
                    ChatInputTextBox.Clear();

                    try
                    {
                        string prompt = "";
                        string appLang = Properties.Settings.Default.AppLanguage;
                        if (string.IsNullOrEmpty(appLang)) appLang = "English";

                        if (exactKey == "Summary")
                        {
                            prompt = $@"You are a supportive academic advisor reviewing the student's Final Exam results.
                                EXAM GRADING REPORT AND CHAT HISTORY: {text}
                                STUDENT SAYS: '{question}'

                                CRITICAL INSTRUCTIONS:
                                1. LANGUAGE: Respond strictly in {appLang.ToUpper()}.
                                2. You are NOT teaching a new lesson. You are here to review the exam.
                                3. If the student asks why they made a mistake, explain the correct answer clearly.
                                4. Give them encouragement and specific study tips based on their performance.
                                5. DO NOT ask 'Which of these topics would you like to explore next?'. Just answer their doubts about the exam.
                                6. FORMATTING: Use **keyword** and __important sentence__.";
                        }
                        else
                        {
                            string instructions = "";

                            if (!string.IsNullOrWhiteSpace(studentPreferences))
                            {
                                instructions = $@"
                                    1. LANGUAGE: Respond strictly in {appLang.ToUpper()}.
                                    2. STRICT TEACHING STYLE: You MUST adapt your behavior EXACTLY to these user preferences: '{studentPreferences}'.
                                    3. ADAPTATION: If the user wants LONG theory, provide massive, highly detailed paragraphs. Do NOT be concise.
                                    4. NO CHOICES: If the user explicitly states they don't like choices or RPG style, DO NOT ask them what to do next. Just automatically transition to the next topic in the syllabus and teach it.
                                    5. MANDATORY FORMATTING: You MUST strictly format crucial keywords using **keyword** and the most important sentences using __important sentence__.
                                    6. Do not repeat topics the student has already learned.";
                            }
                            else
                            {
                                instructions = $@"
                                    1. LANGUAGE: Respond strictly in {appLang.ToUpper()}.
                                    2. DO NOT output a massive wall of text. It's boring. 
                                    3. Teach ONE or TWO core concepts at a time. Explain them deeply and engagingly, using examples.
                                    4. ALWAYS end your response by giving the student a choice. Ask them: 'Which of these topics would you like to explore next?' and provide 2-3 bulleted options based on the syllabus.
                                    5. MANDATORY FORMATTING: You MUST strictly format crucial keywords using **keyword** and the most important sentences using __important sentence__.
                                    6. Do not repeat topics the student has already learned.";
                            }

                                prompt = $@"You are an expert AI professor.
                                        LANGUAGE: Respond strictly in {appLang.ToUpper()}.
                                        TODAY'S MATERIAL AND CHAT HISTORY: {text}
                                        STUDENT SAYS: '{question}'

                                        CRITICAL INSTRUCTIONS:
                                        {instructions}";
                        }

                        string aiResponse = await AiService.GetResponseAsync(prompt);

                        _cancellationTokenSource?.Cancel();
                        _cancellationTokenSource = new System.Threading.CancellationTokenSource();

                        await AnimateAiResponseAsync(aiResponse, false, _cancellationTokenSource.Token);

                        if (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            dayContents[exactKey] += $"\n\n--------------------------------------------------\nStudent: {question}\n\nAI:\n{aiResponse}";
                            SaveCurrentPlanToDisk();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_cancellationTokenSource == null || !_cancellationTokenSource.IsCancellationRequested)
                        {
                            MessageBox.Show($"No connection with Ai: {ex.Message}");
                        }
                    }
                    finally
                    {
                        if (AskAiButton.Content.ToString() == "Stop")
                        {
                            AskAiButton.Content = "Ask AI";
                        }
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

                string appLang = Properties.Settings.Default.AppLanguage;
                if (string.IsNullOrEmpty(appLang)) appLang = "English";

                string prompt = $@"You are an elite University Examiner. 
                CONTEXT: This is a final exam for a course that lasted {totalDays} days, {hoursPerDay} hours per day.

                STUDY MATERIAL:
                {allDaysText}

                INSTRUCTIONS:
                1. LANGUAGE: Generate all questions, instructions, and options strictly in {appLang.ToUpper()}.
                2. Generate EXACTLY {taskCount} tasks.
                3. Analyze the material and pick ONLY 2-3 most relevant TaskTypes from the list: [MultipleChoice, ShortAnswer, Essay, Calculation, CaseStudy, FillInTheBlanks, CodeSnippet].
                   - CRITICAL: If the subject is Humanistic, DO NOT use Calculation or CodeSnippet. 
                   - If the subject is Technical, prioritize Calculation/Code/ShortAnswer.
                4. You MUST return ONLY a valid JSON object. Do not include any extraneous text.

                JSON SCHEMA EXAMPLES:
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

                        SaveCurrentPlanToDisk();

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
            if (currentIndex < 0) return;

            if (!finishedDayIndices.Contains(currentIndex))
            {
                finishedDayIndices.Add(currentIndex);
                finishedDayIndices.Sort();

                if (!string.IsNullOrEmpty(currentProjectPath))
                {
                    ProjectManager.UpdateProjectProgress(currentProjectPath, finishedDayIndices);
                }
            }

            ListBoxItem currentItem = (ListBoxItem)DaysSidebarList.Items[currentIndex];
            string currentTitle = currentItem.Tag?.ToString() ?? "";
            if (currentTitle != "Summary")
            {
                currentItem.Content = $"✅ {currentTitle}";
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

            bool disableTypingAnimation = Properties.Settings.Default.DisableAnimations;
            string[] parts = Regex.Split(response, @"(\*\*.*?\*\*|__.*?__)");
            string keywordHex = Properties.Settings.Default.KeywordColor;
            string sentenceHex = Properties.Settings.Default.SentenceColor;

            if (string.IsNullOrEmpty(keywordHex)) keywordHex = "#DCA550";
            if (string.IsNullOrEmpty(sentenceHex)) sentenceHex = "#4EC9B0";

            var keywordBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(keywordHex);
            var sentenceBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(sentenceHex);

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
                    run.Foreground = keywordBrush;
                }
                else if (part.StartsWith("__") && part.EndsWith("__"))
                {
                    textToPrint = part.Substring(2, part.Length - 4);
                    run.FontStyle = FontStyles.Italic;
                    run.Foreground = sentenceBrush;
                }
                else
                {
                    textToPrint = part;
                    run.Foreground = Brushes.LightGray;
                }

                if (isInitialLoad || disableTypingAnimation)
                {
                    run.Text = textToPrint;
                    StudyContentText.Inlines.Add(run);
                    if (StudyContentText.Parent is ScrollViewer scroll) scroll.ScrollToEnd();
                }
                else
                {
                    int typeSpeed = Properties.Settings.Default.AnimationSpeed;
                    if (typeSpeed <= 0) typeSpeed = 5;

                    StudyContentText.Inlines.Add(run);
                    foreach (char c in textToPrint)
                    {
                        if (token.IsCancellationRequested) break;

                        run.Text += c;
                        if (StudyContentText.Parent is ScrollViewer scroll) scroll.ScrollToEnd();

                        await Task.Delay(typeSpeed);
                    }
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

            TextBlock menuText = (TextBlock)btn.Template.FindName("MenuText", btn);
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
    }
}