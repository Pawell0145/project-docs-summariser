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
                    item.Content = dayTitle;
                    item.Padding = new Thickness(15, 10, 15, 10);
                    DaysSidebarList.Items.Add(item);
                }
            }

            ListBoxItem quizItem = new ListBoxItem();
            quizItem.Content = "Quiz";
            quizItem.Padding = new Thickness(15, 10, 15, 10);
            quizItem.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;
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
                string selectedDay = selectedItem.Content.ToString();

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
    }
}

