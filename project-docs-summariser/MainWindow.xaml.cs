using project_docs_summariser;
using System.Windows;
using System.Windows.Controls;

namespace WpfAiIntegration
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenCreateDialog_Click(object sender, RoutedEventArgs e)
        {
            CreatePlanWindow createWindow = new CreatePlanWindow();
            createWindow.Owner = this;

            bool? result = createWindow.ShowDialog();
            if (result == true && !string.IsNullOrEmpty(createWindow.GeneratedPlan))
            {
                int days = 0;
                int hours = 0;
                int.TryParse(createWindow.DaysTextBox.Text, out days);
                int.TryParse(createWindow.HoursTextBox.Text, out hours);

                StudyWindow studyWindow = new StudyWindow();
                studyWindow.LoadPlan(createWindow.GeneratedPlan, days, hours);
                studyWindow.Show();
            }
        }
    }
}